using System.Text.Json;
using System.Text.Json.Serialization;

namespace OsuGammaChanger.Core;

public sealed class AppConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public List<GammaRange> ArRanges { get; set; } =
    [
        new() { MinAR = 0.0, MaxAR = 8.99, Gamma = 1.0 },
        new() { MinAR = 9.0, MaxAR = 9.49, Gamma = 1.15 },
        new() { MinAR = 9.5, MaxAR = 9.99, Gamma = 1.3 },
        new() { MinAR = 10.0, MaxAR = 11.0, Gamma = 1.45 }
    ];

    public bool RestoreOriginalRampWhenNotPlaying { get; set; } = true;
    public bool RestoreOriginalRampWhenNoRangeMatches { get; set; } = true;
    public bool RestoreOriginalRampOnExit { get; set; } = true;
    public int PollingIntervalMs { get; set; } = 250;
    public string MonitorDeviceName { get; set; } = string.Empty;
    public string OsuSongsDirectory { get; set; } = string.Empty;

    public static AppConfig LoadOrCreate(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        if (!File.Exists(path))
        {
            var created = new AppConfig();
            File.WriteAllText(path, JsonSerializer.Serialize(created, JsonOptions));
            return created;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions)
            ?? throw new InvalidOperationException("Config file deserialized to null.");
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }
}
