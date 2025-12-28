using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using BatchMonitorTools.Commands;
using BatchMonitorTools.Config;
using BatchMonitorTools.Services;
using Microsoft.Win32;

namespace BatchMonitorTools.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ConfigService _configService;
    private readonly string _configPath;
    private readonly AppConfig _appConfig;
    private BatchTaskViewModel? _selectedTask;
    private string _newTaskName = string.Empty;
    private string _newTaskPath = string.Empty;
    private string _newTaskArgs = string.Empty;
    private bool _newTaskAutoStart;
    private bool _startMinimizedToTray;
    private bool _runAtWindowsStartup;
    private bool _autoScrollOutput = true;
    private readonly RelayCommand _addTaskCommand;
    private readonly RelayCommand _removeTaskCommand;
    private readonly RelayCommand _addEmptyTaskCommand;
    private readonly RelayCommand _startAllCommand;
    private readonly RelayCommand _stopAllCommand;
    private readonly RelayCommand _stopAllAndExitCommand;
    private readonly Action _exitApp;

    public MainViewModel(Action? exitApp = null)
    {
        _exitApp = exitApp ?? DefaultExit;
        _configPath = ConfigService.DefaultConfigPath();
        _configService = new ConfigService(_configPath);
        _appConfig = _configService.Load();
        _startMinimizedToTray = _appConfig.StartMinimizedToTray;
        _runAtWindowsStartup = _appConfig.RunAtWindowsStartup;
        _autoScrollOutput = _appConfig.AutoScrollOutput;
        if (_runAtWindowsStartup)
        {
            UpdateStartupRegistration(true);
        }

        Tasks = new ObservableCollection<BatchTaskViewModel>();
        foreach (var task in _appConfig.Tasks)
        {
            if (string.IsNullOrWhiteSpace(task.Name) && string.IsNullOrWhiteSpace(task.Path))
            {
                continue;
            }

            var viewModel = new BatchTaskViewModel(task, new BatchTaskRunner(task));
            Tasks.Add(viewModel);

            if (task.AutoStart)
            {
                viewModel.StartTask();
            }
        }

        _addTaskCommand = new RelayCommand(AddTask, CanAddTask);
        _removeTaskCommand = new RelayCommand(RemoveTask, () => SelectedTask != null);
        _addEmptyTaskCommand = new RelayCommand(AddEmptyTask);
        _startAllCommand = new RelayCommand(StartAll);
        _stopAllCommand = new RelayCommand(StopAll);
        SaveConfigCommand = new RelayCommand(SaveConfig);
        _stopAllAndExitCommand = new RelayCommand(StopAllAndExit);
    }

    public ObservableCollection<BatchTaskViewModel> Tasks { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public BatchTaskViewModel? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (_selectedTask == value)
            {
                return;
            }

            _selectedTask = value;
            OnPropertyChanged();
            _removeTaskCommand.RaiseCanExecuteChanged();
        }
    }

    public string NewTaskName
    {
        get => _newTaskName;
        set
        {
            if (_newTaskName == value)
            {
                return;
            }

            _newTaskName = value;
            OnPropertyChanged();
        }
    }

    public string NewTaskPath
    {
        get => _newTaskPath;
        set
        {
            if (_newTaskPath == value)
            {
                return;
            }

            _newTaskPath = value;
            OnPropertyChanged();
            _addTaskCommand.RaiseCanExecuteChanged();
        }
    }

    public string NewTaskArgs
    {
        get => _newTaskArgs;
        set
        {
            if (_newTaskArgs == value)
            {
                return;
            }

            _newTaskArgs = value;
            OnPropertyChanged();
        }
    }

    public bool NewTaskAutoStart
    {
        get => _newTaskAutoStart;
        set
        {
            if (_newTaskAutoStart == value)
            {
                return;
            }

            _newTaskAutoStart = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand AddTaskCommand => _addTaskCommand;

    public RelayCommand RemoveTaskCommand => _removeTaskCommand;

    public RelayCommand AddEmptyTaskCommand => _addEmptyTaskCommand;

    public RelayCommand StartAllCommand => _startAllCommand;

    public RelayCommand StopAllCommand => _stopAllCommand;

    public RelayCommand SaveConfigCommand { get; }

    public RelayCommand StopAllAndExitCommand => _stopAllAndExitCommand;

    public bool StartMinimizedToTray
    {
        get => _startMinimizedToTray;
        set
        {
            if (_startMinimizedToTray == value)
            {
                return;
            }

            _startMinimizedToTray = value;
            _appConfig.StartMinimizedToTray = value;
            OnPropertyChanged();
            SaveConfig();
        }
    }

    public bool RunAtWindowsStartup
    {
        get => _runAtWindowsStartup;
        set
        {
            if (_runAtWindowsStartup == value)
            {
                return;
            }

            _runAtWindowsStartup = value;
            _appConfig.RunAtWindowsStartup = value;
            OnPropertyChanged();
            UpdateStartupRegistration(value);
            SaveConfig();
        }
    }

    public bool AutoScrollOutput
    {
        get => _autoScrollOutput;
        set
        {
            if (_autoScrollOutput == value)
            {
                return;
            }

            _autoScrollOutput = value;
            _appConfig.AutoScrollOutput = value;
            OnPropertyChanged();
            SaveConfig();
        }
    }

    private bool CanAddTask()
    {
        return !string.IsNullOrWhiteSpace(NewTaskPath);
    }

    private void AddTask()
    {
        var path = NewTaskPath.Trim();
        var name = string.IsNullOrWhiteSpace(NewTaskName)
            ? Path.GetFileNameWithoutExtension(path)
            : NewTaskName.Trim();

        var config = new BatchTaskConfig
        {
            Name = name,
            Path = path,
            Args = NewTaskArgs?.Trim() ?? string.Empty,
            AutoStart = NewTaskAutoStart
        };

        var viewModel = new BatchTaskViewModel(config, new BatchTaskRunner(config));
        Tasks.Add(viewModel);
        SelectedTask = viewModel;

        if (config.AutoStart)
        {
            viewModel.StartTask();
        }

        NewTaskName = string.Empty;
        NewTaskPath = string.Empty;
        NewTaskArgs = string.Empty;
        NewTaskAutoStart = false;

        SaveConfig();
    }

    private void AddEmptyTask()
    {
        var config = new BatchTaskConfig
        {
            Name = "New Task",
            Path = string.Empty,
            Args = string.Empty,
            AutoStart = false,
            MaxOutputLines = 500
        };

        var viewModel = new BatchTaskViewModel(config, new BatchTaskRunner(config));
        Tasks.Add(viewModel);
        SelectedTask = viewModel;
    }

    private void RemoveTask()
    {
        if (SelectedTask == null)
        {
            return;
        }

        var toRemove = SelectedTask;
        toRemove.StopTask();
        SelectedTask = null;
        Tasks.Remove(toRemove);
        SaveConfig();
    }

    private void SaveConfig()
    {
        _appConfig.Tasks.Clear();
        foreach (var task in Tasks)
        {
            _appConfig.Tasks.Add(task.Config);
        }

        _configService.Save(_appConfig);
    }

    private void StartAll()
    {
        foreach (var task in Tasks)
        {
            task.StartTask();
        }
    }

    private void StopAll()
    {
        foreach (var task in Tasks)
        {
            task.StopTask();
        }
    }

    private void StopAllAndExit()
    {
        foreach (var task in Tasks)
        {
            task.StopTask();
        }

        SaveConfig();
        _exitApp();
    }

    private static void DefaultExit()
    {
        System.Windows.Application.Current?.Shutdown();
    }

    private void UpdateStartupRegistration(bool enable)
    {
        try
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(
                "Software\\Microsoft\\Windows\\CurrentVersion\\Run",
                writable: true);
            if (runKey == null)
            {
                return;
            }

            const string appName = "BatchMonitorTools";
            if (enable)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(exePath))
                {
                    runKey.SetValue(appName, $"\"{exePath}\"");
                }
            }
            else
            {
                runKey.DeleteValue(appName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Keep UI responsive even if registry access fails.
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
