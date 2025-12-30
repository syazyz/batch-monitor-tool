using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
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
    private readonly ConcurrentQueue<string> _pendingOutput;
    private readonly DispatcherTimer _flushTimer;
    private readonly ITaskRunner _runner;
    private readonly Config.BatchTaskConfig _config;

    public BatchTaskViewModel(Config.BatchTaskConfig config, ITaskRunner runner)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _name = runner.Name;
        _isRunning = false;
        _maxOutputLines = config.MaxOutputLines > 0 ? config.MaxOutputLines : 500;
        _config.MaxOutputLines = _maxOutputLines;
        _pendingOutput = new ConcurrentQueue<string>();
        OutputLines = new ObservableCollection<string>();

        StartCommand = new RelayCommand(Start, () => !IsRunning);
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        ClearOutputCommand = new RelayCommand(ClearOutput);
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

    public Config.BatchTaskConfig Config => _config;

    public ObservableCollection<string> OutputLines { get; }

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
            OutputLines.Add(line);
        }

        TrimOutputLines();
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

    private void ClearOutput()
    {
        while (_pendingOutput.TryDequeue(out _))
        {
        }

        OutputLines.Clear();
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
