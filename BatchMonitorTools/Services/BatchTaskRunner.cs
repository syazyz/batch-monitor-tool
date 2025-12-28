using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BatchMonitorTools.Config;

namespace BatchMonitorTools.Services;

// Runs a batch file via cmd.exe and streams stdout/stderr back to the UI.
public sealed class BatchTaskRunner : ITaskRunner
{
    private const uint CtrlCEvent = 0;
    private const int SoftStopTimeoutMs = 2000;

    private readonly BatchTaskConfig _config;
    private Process? _process;
    private readonly object _sync = new();

    public BatchTaskRunner(BatchTaskConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        Name = string.IsNullOrWhiteSpace(config.Name) ? "Batch Task" : config.Name;
    }

    public string Name { get; }

    public bool IsRunning { get; private set; }

    public event Action<string>? OutputReceived;

    public event Action<int?>? Exited;

    public void Start()
    {
        lock (_sync)
        {
            if (_process is { HasExited: false })
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_config.Path) || !File.Exists(_config.Path))
            {
                OutputReceived?.Invoke($"Batch file not found: {_config.Path}");
                IsRunning = false;
                return;
            }

            try
            {
                var workingDir = Path.GetDirectoryName(_config.Path) ?? Environment.CurrentDirectory;
                var args = string.IsNullOrWhiteSpace(_config.Args) ? string.Empty : $" {_config.Args}";
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{_config.Path}\"{args}",
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
                _process.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        OutputReceived?.Invoke(e.Data);
                    }
                };
                _process.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        OutputReceived?.Invoke($"[err] {e.Data}");
                    }
                };
                _process.Exited += (_, _) => HandleExited();

                if (_process.Start())
                {
                    IsRunning = true;
                    _process.BeginOutputReadLine();
                    _process.BeginErrorReadLine();
                }
                else
                {
                    OutputReceived?.Invoke("Failed to start process.");
                    IsRunning = false;
                }
            }
            catch (Exception ex)
            {
                OutputReceived?.Invoke($"Start failed: {ex.Message}");
                IsRunning = false;
            }
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            if (_process is null)
            {
                return;
            }

            try
            {
                if (!_process.HasExited)
                {
                    // Try Ctrl+C first to allow cleanup; fall back to kill.
                    if (TrySendCtrlC(_process))
                    {
                        var process = _process;
                        Task.Run(() =>
                        {
                            if (!process.WaitForExit(SoftStopTimeoutMs))
                            {
                                try
                                {
                                    process.Kill(entireProcessTree: true);
                                }
                                catch (Exception ex)
                                {
                                    OutputReceived?.Invoke($"Stop failed: {ex.Message}");
                                }
                            }
                        });
                        return;
                    }

                    _process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex)
            {
                OutputReceived?.Invoke($"Stop failed: {ex.Message}");
            }
        }
    }

    private void HandleExited()
    {
        int? exitCode = null;
        lock (_sync)
        {
            if (_process != null)
            {
                try
                {
                    if (_process.HasExited)
                    {
                        exitCode = _process.ExitCode;
                    }
                }
                catch
                {
                    exitCode = null;
                }
            }

            IsRunning = false;
            _process?.Dispose();
            _process = null;
        }

        Exited?.Invoke(exitCode);
    }

    private static bool TrySendCtrlC(Process process)
    {
        try
        {
            FreeConsole();
            if (!AttachConsole((uint)process.Id))
            {
                return false;
            }

            SetConsoleCtrlHandler(null, true);
            var sent = GenerateConsoleCtrlEvent(CtrlCEvent, 0);
            SetConsoleCtrlHandler(null, false);
            FreeConsole();

            return sent;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    private delegate bool ConsoleCtrlDelegate(uint ctrlType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? handler, bool add);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
}
