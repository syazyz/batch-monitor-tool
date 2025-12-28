using System.Diagnostics;
using System.Windows;
using BatchMonitorTools.ViewModels;
using DrawingIcon = System.Drawing.Icon;
using DrawingSystemIcons = System.Drawing.SystemIcons;
using Forms = System.Windows.Forms;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace BatchMonitorTools;

public partial class MainWindow : Window
{
    private readonly Forms.NotifyIcon _trayIcon;
    private bool _isExitRequested;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(RequestExit);

        _trayIcon = new Forms.NotifyIcon
        {
            Text = "Batch Monitor Tools",
            Icon = GetTrayIcon(),
            Visible = false
        };

        var menu = new Forms.ContextMenuStrip();
        var restoreItem = new Forms.ToolStripMenuItem("恢复窗口");
        restoreItem.Click += (_, _) => RestoreFromTray();
        var exitItem = new Forms.ToolStripMenuItem("彻底关闭程序");
        exitItem.Click += (_, _) => ExitFromTray();
        menu.Items.Add(restoreItem);
        menu.Items.Add(exitItem);
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();

        Closing += OnClosing;
        Loaded += OnLoaded;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExitRequested)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            return;
        }

        e.Cancel = true;
        Hide();
        _trayIcon.Visible = true;
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        _trayIcon.Visible = false;
    }

    private void ExitFromTray()
    {
        if (DataContext is MainViewModel viewModel && viewModel.StopAllAndExitCommand.CanExecute(null))
        {
            viewModel.StopAllAndExitCommand.Execute(null);
            return;
        }

        RequestExit();
    }

    private void RequestExit()
    {
        _isExitRequested = true;
        System.Windows.Application.Current?.Shutdown();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.StartMinimizedToTray)
        {
            Hide();
            _trayIcon.Visible = true;
        }
    }

    private void OutputTextChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.AutoScrollOutput)
        {
            if (sender is WpfTextBox textBox)
            {
                textBox.ScrollToEnd();
            }
        }
    }

    private static DrawingIcon GetTrayIcon()
    {
        var modulePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrWhiteSpace(modulePath))
        {
            var icon = DrawingIcon.ExtractAssociatedIcon(modulePath);
            if (icon != null)
            {
                return icon;
            }
        }

        return DrawingSystemIcons.Application;
    }
}
