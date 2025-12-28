using System;

namespace BatchMonitorTools.Services;

public interface ITaskRunner
{
    string Name { get; }
    bool IsRunning { get; }
    event Action<string>? OutputReceived;
    event Action<int?>? Exited;
    void Start();
    void Stop();
}
