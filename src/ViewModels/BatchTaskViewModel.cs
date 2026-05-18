using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Threading;
using BatchMonitorTools.Commands;
using BatchMonitorTools.Services;

namespace BatchMonitorTools.ViewModels;

// View model for a single batch task and its live output.
public sealed class BatchTaskViewModel : INotifyPropertyChanged
{
    private string _name;
    private bool _isRunning;
    private int _maxOutputLines;
    private string _searchQuery = string.Empty;
    private string _filterQueryDraft = string.Empty;
    private string _activeFilterQuery = string.Empty;
    private bool _outputHighlightMatches = true;
    private OutputModeOption _selectedOutputMode;
    private OutputLineViewModel? _selectedOutputLine;
    private readonly ConcurrentQueue<string> _pendingOutput;
    private readonly DispatcherTimer _flushTimer;
    private readonly ITaskRunner _runner;
    private readonly Config.BatchTaskConfig _config;
    private readonly RelayCommand _applyFilterCommand;
    private readonly RelayCommand _clearFilterCommand;
    private readonly RelayCommand _findNextMatchCommand;
    private readonly RelayCommand _findPreviousMatchCommand;

    public BatchTaskViewModel(Config.BatchTaskConfig config, ITaskRunner runner)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _name = runner.Name;
        _isRunning = false;
        _maxOutputLines = config.MaxOutputLines > 0 ? config.MaxOutputLines : 500;
        _config.MaxOutputLines = _maxOutputLines;
        _pendingOutput = new ConcurrentQueue<string>();
        OutputLines = new ObservableCollection<OutputLineViewModel>();
        OutputModes = new ObservableCollection<OutputModeOption>
        {
            new OutputModeOption(OutputQueryMode.Search, "Search"),
            new OutputModeOption(OutputQueryMode.Filter, "Filter")
        };
        _selectedOutputMode = OutputModes[0];
        OutputView = CollectionViewSource.GetDefaultView(OutputLines);
        // Filtering is only active in filter mode, with the applied filter query.
        OutputView.Filter = FilterOutputLine;

        StartCommand = new RelayCommand(Start, () => !IsRunning);
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        ClearOutputCommand = new RelayCommand(ClearOutput);
        _applyFilterCommand = new RelayCommand(ApplyFilter, () => IsFilterMode);
        _clearFilterCommand = new RelayCommand(ClearFilter, CanClearFilter);
        _findPreviousMatchCommand = new RelayCommand(() => SelectMatch(forward: false), CanNavigateMatches);
        _findNextMatchCommand = new RelayCommand(() => SelectMatch(forward: true), CanNavigateMatches);
        UpdateDerivedProperties();

        // Flush output in batches to avoid per-line UI updates.
        _flushTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };
        _flushTimer.Tick += (_, _) => FlushPendingOutput();
        _flushTimer.Start();

        _runner.OutputReceived += OnOutputReceived;
        _runner.Exited += OnExited;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            _config.Name = value;
            OnPropertyChanged();
            UpdateDerivedProperties();
        }
    }

    public string Path
    {
        get => _config.Path;
        set
        {
            if (_config.Path == value)
            {
                return;
            }

            _config.Path = value;
            OnPropertyChanged();
        }
    }

    public string Args
    {
        get => _config.Args;
        set
        {
            if (_config.Args == value)
            {
                return;
            }

            _config.Args = value;
            OnPropertyChanged();
        }
    }

    public bool AutoStart
    {
        get => _config.AutoStart;
        set
        {
            if (_config.AutoStart == value)
            {
                return;
            }

            _config.AutoStart = value;
            OnPropertyChanged();
        }
    }

    public bool EnableInputRedirect
    {
        get => _config.EnableInputRedirect;
        set
        {
            if (_config.EnableInputRedirect == value)
            {
                return;
            }

            _config.EnableInputRedirect = value;
            OnPropertyChanged();
        }
    }

    public Config.BatchTaskConfig Config => _config;

    public ObservableCollection<OutputLineViewModel> OutputLines { get; }

    public ICollectionView OutputView { get; }

    public ObservableCollection<OutputModeOption> OutputModes { get; }

    public OutputModeOption SelectedOutputMode
    {
        get => _selectedOutputMode;
        set
        {
            if (_selectedOutputMode == value)
            {
                return;
            }

            _selectedOutputMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsFilterMode));
            OnPropertyChanged(nameof(IsSearchMode));
            OnPropertyChanged(nameof(OutputQuery));
            OnPropertyChanged(nameof(OutputHighlightQuery));
            RefreshOutputMatches();
            RaiseCommandStateChanged();
        }
    }

    public bool IsFilterMode => SelectedOutputMode.Mode == OutputQueryMode.Filter;

    public bool IsSearchMode => SelectedOutputMode.Mode == OutputQueryMode.Search;

    public string OutputQuery
    {
        get => IsFilterMode ? _filterQueryDraft : _searchQuery;
        set
        {
            if (IsFilterMode)
            {
                if (_filterQueryDraft == value)
                {
                    return;
                }

                _filterQueryDraft = value ?? string.Empty;
                OnPropertyChanged();
                _clearFilterCommand.RaiseCanExecuteChanged();
                return;
            }

            if (_searchQuery == value)
            {
                return;
            }

            _searchQuery = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OutputHighlightQuery));
            RefreshOutputMatches();
        }
    }

    public bool OutputHighlightMatches
    {
        get => _outputHighlightMatches;
        set
        {
            if (_outputHighlightMatches == value)
            {
                return;
            }

            _outputHighlightMatches = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OutputHighlightQuery));
        }
    }

    public string OutputHighlightQuery => OutputHighlightMatches ? EffectiveQuery : string.Empty;

    public OutputLineViewModel? SelectedOutputLine
    {
        get => _selectedOutputLine;
        set
        {
            if (_selectedOutputLine == value)
            {
                return;
            }

            _selectedOutputLine = value;
            OnPropertyChanged();
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (_isRunning == value)
            {
                return;
            }

            _isRunning = value;
            OnPropertyChanged();
            UpdateDerivedProperties();
            RaiseCommandStateChanged();
        }
    }

    public string StatusText { get; private set; } = string.Empty;

    public string HeaderText { get; private set; } = string.Empty;

    public ICommand StartCommand { get; }

    public ICommand StopCommand { get; }

    public ICommand ClearOutputCommand { get; }

    public ICommand ApplyFilterCommand => _applyFilterCommand;

    public ICommand ClearFilterCommand => _clearFilterCommand;

    public ICommand FindPreviousMatchCommand => _findPreviousMatchCommand;

    public ICommand FindNextMatchCommand => _findNextMatchCommand;

    public int MaxOutputLines
    {
        get => _maxOutputLines;
        set
        {
            if (_maxOutputLines == value)
            {
                return;
            }

            _maxOutputLines = value;
            _config.MaxOutputLines = value;
            OnPropertyChanged();
            TrimOutputLines();
        }
    }

    public void StartTask() => Start();

    public void StopTask() => Stop();

    private void Start()
    {
        _runner.Start();
        IsRunning = _runner.IsRunning;
        QueueOutputLine(IsRunning ? $"{Name} started." : $"{Name} failed to start.");
    }

    private void Stop()
    {
        _runner.Stop();
        IsRunning = _runner.IsRunning;
        QueueOutputLine($"{Name} stopped.");
    }

    private void UpdateDerivedProperties()
    {
        StatusText = IsRunning ? "Status: Running" : "Status: Stopped";
        HeaderText = IsRunning ? $"{Name} (Running)" : Name;
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(HeaderText));
    }

    private void RaiseCommandStateChanged()
    {
        if (StartCommand is RelayCommand startCommand)
        {
            startCommand.RaiseCanExecuteChanged();
        }

        if (StopCommand is RelayCommand stopCommand)
        {
            stopCommand.RaiseCanExecuteChanged();
        }

        _applyFilterCommand.RaiseCanExecuteChanged();
        _clearFilterCommand.RaiseCanExecuteChanged();
        _findPreviousMatchCommand.RaiseCanExecuteChanged();
        _findNextMatchCommand.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void OnOutputReceived(string line)
    {
        QueueOutputLine(line);
    }

    private void OnExited(int? exitCode)
    {
        PostToUi(() =>
        {
            IsRunning = _runner.IsRunning;
            QueueOutputLine($"Process exited (code {(exitCode.HasValue ? exitCode.Value.ToString() : "n/a")}).");
        });
    }

    private void QueueOutputLine(string line)
    {
        _pendingOutput.Enqueue(line ?? string.Empty);
    }

    private void FlushPendingOutput()
    {
        if (_pendingOutput.IsEmpty)
        {
            return;
        }

        var batch = new List<string>();
        while (_pendingOutput.TryDequeue(out var line))
        {
            batch.Add(line);
        }

        if (batch.Count == 0)
        {
            return;
        }

        foreach (var line in batch)
        {
            var outputLine = new OutputLineViewModel(line);
            UpdateLineMatch(outputLine, EffectiveQuery);
            OutputLines.Add(outputLine);
        }

        TrimOutputLines();
        _findPreviousMatchCommand.RaiseCanExecuteChanged();
        _findNextMatchCommand.RaiseCanExecuteChanged();
    }

    private void TrimOutputLines()
    {
        if (MaxOutputLines <= 0)
        {
            return;
        }

        // Keep only the most recent MaxOutputLines entries.
        var excess = OutputLines.Count - MaxOutputLines;
        if (excess <= 0)
        {
            return;
        }

        for (var i = 0; i < excess; i++)
        {
            OutputLines.RemoveAt(0);
        }
    }

    private string EffectiveQuery => IsFilterMode ? _activeFilterQuery : _searchQuery;

    private void ApplyFilter()
    {
        _activeFilterQuery = _filterQueryDraft;
        RefreshOutputMatches();
    }

    private void ClearFilter()
    {
        _filterQueryDraft = string.Empty;
        _activeFilterQuery = string.Empty;
        OnPropertyChanged(nameof(OutputQuery));
        RefreshOutputMatches();
    }

    private bool CanClearFilter()
    {
        return IsFilterMode
            && (!string.IsNullOrWhiteSpace(_filterQueryDraft) || !string.IsNullOrWhiteSpace(_activeFilterQuery));
    }

    private void SelectMatch(bool forward)
    {
        if (!IsSearchMode || string.IsNullOrWhiteSpace(_searchQuery) || OutputLines.Count == 0)
        {
            return;
        }

        var startIndex = SelectedOutputLine != null ? OutputLines.IndexOf(SelectedOutputLine) : (forward ? -1 : OutputLines.Count);
        for (var offset = 0; offset < OutputLines.Count; offset++)
        {
            var index = forward
                ? (startIndex + 1 + offset) % OutputLines.Count
                : (startIndex - 1 - offset + OutputLines.Count * 2) % OutputLines.Count;

            if (OutputLines[index].IsMatch)
            {
                SelectedOutputLine = OutputLines[index];
                return;
            }
        }
    }

    private bool CanNavigateMatches()
    {
        return IsSearchMode
            && !string.IsNullOrWhiteSpace(_searchQuery)
            && OutputLines.Any(line => line.IsMatch);
    }

    private void RefreshOutputMatches()
    {
        var query = EffectiveQuery;
        foreach (var line in OutputLines)
        {
            UpdateLineMatch(line, query);
        }

        OnPropertyChanged(nameof(OutputHighlightQuery));
        OutputView.Refresh();
        RaiseCommandStateChanged();
    }

    private void UpdateLineMatch(OutputLineViewModel line, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            line.IsMatch = false;
            return;
        }

        line.IsMatch = IsLineMatch(line.Text, query);
    }

    private static bool IsLineMatch(string text, string query)
    {
        return text?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool FilterOutputLine(object item)
    {
        if (item is not OutputLineViewModel line)
        {
            return false;
        }

        if (!IsFilterMode || string.IsNullOrWhiteSpace(_activeFilterQuery))
        {
            return true;
        }

        return line.IsMatch;
    }

    private void ClearOutput()
    {
        while (_pendingOutput.TryDequeue(out _))
        {
        }

        OutputLines.Clear();
        SelectedOutputLine = null;
        RaiseCommandStateChanged();
    }

    private static void PostToUi(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            return;
        }

        if (dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.BeginInvoke(action);
    }
}

public enum OutputQueryMode
{
    Search,
    Filter
}

public sealed class OutputModeOption
{
    public OutputModeOption(OutputQueryMode mode, string label)
    {
        Mode = mode;
        Label = label;
    }

    public OutputQueryMode Mode { get; }

    public string Label { get; }
}

public sealed class OutputLineViewModel : INotifyPropertyChanged
{
    private bool _isMatch;

    public OutputLineViewModel(string text)
    {
        Text = text ?? string.Empty;
    }

    public string Text { get; }

    public bool IsMatch
    {
        get => _isMatch;
        set
        {
            if (_isMatch == value)
            {
                return;
            }

            _isMatch = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMatch)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
