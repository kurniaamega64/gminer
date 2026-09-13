using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gminer.Miner.Device;

public sealed class DeviceConfig
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "cpu";

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("threads")]
    public int? Threads { get; set; }

    [JsonPropertyName("intensity")]
    public double Intensity { get; set; } = 1.0;

    [JsonPropertyName("affinity")]
    public int? CpuAffinity { get; set; }

    [JsonPropertyName("memory_clock")]
    public int? MemoryClockMhz { get; set; }

    [JsonPropertyName("core_clock")]
    public int? CoreClockMhz { get; set; }

    [JsonPropertyName("power_limit")]
    public int? PowerLimitWatts { get; set; }

    public static DeviceConfig CpuDefault() => new()
    {
        Type = "cpu",
        Index = 0,
        Threads = Math.Max(1, Environment.ProcessorCount - 1),
        Intensity = 1.0,
    };

    public static DeviceConfig GpuDefault(int index) => new()
    {
        Type = "gpu",
        Index = index,
        Intensity = 0.8,
    };

    public static List<DeviceConfig> LoadFromJson(string json) =>
        JsonSerializer.Deserialize<List<DeviceConfig>>(json) ?? [];

    public string ToJson() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
}
