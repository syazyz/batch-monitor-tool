namespace BatchMonitorTools.Config;

// Per-task settings persisted to config.json.
public sealed class BatchTaskConfig
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Args { get; set; } = string.Empty;
    public bool AutoStart { get; set; }
    public int MaxOutputLines { get; set; } = 500;
}
