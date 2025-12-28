using System;

namespace BatchMonitorTools.Services;

// Abstraction for something that can run/stop and stream output.
public interface ITaskRunner
{
    string Name { get; }
    bool IsRunning { get; }
    event Action<string>? OutputReceived;
    event Action<int?>? Exited;
    void Start();
    void Stop();
}
