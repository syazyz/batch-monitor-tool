using System;
using System.IO;
using System.Text.Json;
using BatchMonitorTools.Config;

namespace BatchMonitorTools.Services;

// Loads/saves app configuration from a fixed path.
public sealed class ConfigService
{
    private readonly string _configPath;

    public ConfigService(string configPath)
    {
        _configPath = configPath;
    }

    public AppConfig Load()
    {
        if (!File.Exists(_configPath))
        {
            return new AppConfig();
        }

        var json = File.ReadAllText(_configPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();
    }

    public void Save(AppConfig config)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(config, options);
        File.WriteAllText(_configPath, json);
    }

    public static string DefaultConfigPath()
    {
        // Keep config alongside the executable for easy portability.
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, "config.json");
    }
}
