using System.Collections.Generic;

namespace BatchMonitorTools.Config;

// Root app settings persisted to config.json.
public sealed class AppConfig
{
    public List<BatchTaskConfig> Tasks { get; set; } = new();
    public bool StartMinimizedToTray { get; set; }
    public bool RunAtWindowsStartup { get; set; }
    public bool AutoScrollOutput { get; set; } = true;
}
