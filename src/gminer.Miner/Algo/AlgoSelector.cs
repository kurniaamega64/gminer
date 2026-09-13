namespace Gminer.Miner.Algo;

public enum AlgorithmKind
{
    RandomX,
    Ethash,
    KawPow,
    SHA256,
    Scrypt,
    Equihash,
    CryptoNight,
}

public sealed record AlgorithmInfo(AlgorithmKind Kind, string Name, string CoinSymbol, int BlockTime);

public static class AlgoSelector
{
    private static readonly Dictionary<string, AlgorithmInfo> Registry =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["randomx"]     = new(AlgorithmKind.RandomX,     "RandomX",     "XMR", 120),
            ["ethash"]      = new(AlgorithmKind.Ethash,       "Ethash",      "ETH",  12),
            ["kawpow"]      = new(AlgorithmKind.KawPow,       "KawPow",      "RVN",  60),
            ["sha256"]      = new(AlgorithmKind.SHA256,       "SHA-256d",    "BTC", 600),
            ["scrypt"]      = new(AlgorithmKind.Scrypt,       "Scrypt",      "LTC", 150),
            ["equihash"]    = new(AlgorithmKind.Equihash,     "Equihash",    "ZEC",  75),
            ["cryptonight"] = new(AlgorithmKind.CryptoNight,  "CryptoNight", "XMR", 120),
        };

    public static AlgorithmInfo Resolve(string name)
    {
        if (Registry.TryGetValue(name, out var info))
            return info;
        throw new NotSupportedException(
            $"Unknown algorithm: {name}. Supported: {string.Join(", ", Registry.Keys)}");
    }

    public static IReadOnlyList<AlgorithmInfo> ListAll() =>
        Registry.Values.ToList().AsReadOnly();

    public static bool IsSupported(string name) =>
        Registry.ContainsKey(name);

    public static AlgorithmInfo Default => Registry["sha256"];
}
