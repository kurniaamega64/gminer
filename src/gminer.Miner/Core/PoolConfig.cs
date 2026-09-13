using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gminer.Miner.Core;

public sealed class PoolConfig
{
    [JsonPropertyName("host")]
    public string Host { get; set; } = "pool.example.com";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 3333;

    [JsonPropertyName("wallet")]
    public string Wallet { get; set; } = string.Empty;

    [JsonPropertyName("worker")]
    public string Worker { get; set; } = "default";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "x";

    [JsonPropertyName("tls")]
    public bool UseTls { get; set; }

    [JsonPropertyName("keepalive")]
    public bool Keepalive { get; set; } = true;

    [JsonPropertyName("nicehash")]
    public bool NiceHash { get; set; }

    [JsonPropertyName("algo")]
    public string Algorithm { get; set; } = "sha256";

    [JsonPropertyName("donate_percent")]
    public double DonatePercent { get; set; } = 1.0;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new ArgumentException("Pool host is required");
        if (Port is < 1 or > 65535)
            throw new ArgumentException("Port must be between 1 and 65535");
        if (string.IsNullOrWhiteSpace(Wallet))
            throw new ArgumentException("Wallet address is required");
    }

    public string ToJson() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public static PoolConfig FromJson(string json) =>
        JsonSerializer.Deserialize<PoolConfig>(json) ?? throw new FormatException("Invalid pool config");

    public static PoolConfig FromArgs(string host, int port, string wallet, string? worker = null)
    {
        var cfg = new PoolConfig { Host = host, Port = port, Wallet = wallet };
        if (worker is not null) cfg.Worker = worker;
        cfg.Validate();
        return cfg;
    }

    public override string ToString() =>
        $"stratum+tcp://{Host}:{Port} wallet={Wallet} worker={Worker} tls={UseTls}";
}
