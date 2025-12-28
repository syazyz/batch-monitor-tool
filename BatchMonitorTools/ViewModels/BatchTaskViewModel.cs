using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BatchMonitorTools.Commands;
using BatchMonitorTools.Services;

namespace BatchMonitorTools.ViewModels;

// View model for a single batch task and its live output.
public sealed class BatchTaskViewModel : INotifyPropertyChanged
{
    private string _name;
    private string _outputText;
    private bool _isRunning;
    private int _maxOutputLines;
    private readonly List<string> _outputLines;
    private readonly ITaskRunner _runner;
    private readonly Config.BatchTaskConfig _config;

    public BatchTaskViewModel(Config.BatchTaskConfig config, ITaskRunner runner)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _name = runner.Name;
        _outputText = string.Empty;
        _isRunning = false;
        _maxOutputLines = config.MaxOutputLines > 0 ? config.MaxOutputLines : 500;
        _config.MaxOutputLines = _maxOutputLines;
        _outputLines = new List<string>();

        StartCommand = new RelayCommand(Start, () => !IsRunning);
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        ClearOutputCommand = new RelayCommand(ClearOutput);
        UpdateDerivedProperties();

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

    public string OutputText
    {
        get => _outputText;
        set
        {
            if (_outputText == value)
            {
                return;
            }

            _outputText = value;
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
            RebuildOutputText();
        }
    }

    public void StartTask() => Start();

    public void StopTask() => Stop();

    private void Start()
    {
        _runner.Start();
        IsRunning = _runner.IsRunning;
        AppendOutput(IsRunning ? $"{Name} started." : $"{Name} failed to start.");
    }

    private void Stop()
    {
        _runner.Stop();
        IsRunning = _runner.IsRunning;
        AppendOutput($"{Name} stopped.");
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
        PostToUi(() => AppendOutput(line));
    }

    private void OnExited(int? exitCode)
    {
        PostToUi(() =>
        {
            IsRunning = _runner.IsRunning;
            AppendOutput($"Process exited (code {(exitCode.HasValue ? exitCode.Value.ToString() : "n/a")}).");
        });
    }

    private void AppendOutput(string line)
    {
        _outputLines.Add(line);
        TrimOutputLines();
        RebuildOutputText();
    }

    private void TrimOutputLines()
    {
        if (MaxOutputLines <= 0)
        {
            return;
        }

        // Keep only the most recent MaxOutputLines entries.
        var excess = _outputLines.Count - MaxOutputLines;
        if (excess <= 0)
        {
            return;
        }

        _outputLines.RemoveRange(0, excess);
    }

    private void RebuildOutputText()
    {
        if (_outputLines.Count == 0)
        {
            OutputText = string.Empty;
            return;
        }

        OutputText = string.Join(System.Environment.NewLine, _outputLines) + System.Environment.NewLine;
    }

    private void ClearOutput()
    {
        _outputLines.Clear();
        OutputText = string.Empty;
    }

    private static void PostToUi(Action action)
    {
        if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == true)
        {
            action();
            return;
        }

        System.Windows.Application.Current?.Dispatcher?.Invoke(action);
    }
}
