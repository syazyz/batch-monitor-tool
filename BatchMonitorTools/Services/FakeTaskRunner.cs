using System;
using System.Timers;

namespace BatchMonitorTools.Services;

public sealed class FakeTaskRunner : ITaskRunner
{
    private readonly System.Timers.Timer _timer;
    private int _tick;

    public FakeTaskRunner(string name, double intervalMs = 1200)
    {
        Name = name;
        _timer = new System.Timers.Timer(intervalMs);
        _timer.Elapsed += (_, _) => EmitOutput();
    }

    public string Name { get; }

    public bool IsRunning { get; private set; }

    public event Action<string>? OutputReceived;

    public event Action<int?>? Exited;

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        _timer.Start();
        OutputReceived?.Invoke($"[{Name}] heartbeat started.");
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        _timer.Stop();
        IsRunning = false;
        OutputReceived?.Invoke($"[{Name}] heartbeat stopped.");
        Exited?.Invoke(null);
    }

    private void EmitOutput()
    {
        if (!IsRunning)
        {
            return;
        }

        _tick++;
        OutputReceived?.Invoke($"[{Name}] tick {_tick}");
    }
}
