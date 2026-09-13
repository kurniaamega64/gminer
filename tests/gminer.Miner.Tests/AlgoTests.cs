using Xunit;
using Gminer.Miner.Algo;

namespace Gminer.Miner.Tests;

public class AlgoTests
{
    [Theory]
    [InlineData("sha256",      AlgorithmKind.SHA256)]
    [InlineData("randomx",     AlgorithmKind.RandomX)]
    [InlineData("ethash",      AlgorithmKind.Ethash)]
    [InlineData("kawpow",      AlgorithmKind.KawPow)]
    [InlineData("scrypt",      AlgorithmKind.Scrypt)]
    [InlineData("equihash",    AlgorithmKind.Equihash)]
    [InlineData("cryptonight", AlgorithmKind.CryptoNight)]
    public void Resolve_ReturnsCorrectKind(string name, AlgorithmKind expected)
    {
        var info = AlgoSelector.Resolve(name);
        Assert.Equal(expected, info.Kind);
    }

    [Fact]
    public void Resolve_IsCaseInsensitive()
    {
        var lo = AlgoSelector.Resolve("sha256");
        var up = AlgoSelector.Resolve("SHA256");
        Assert.Equal(lo.Kind, up.Kind);
    }

    [Fact]
    public void Resolve_ThrowsOnUnknown()
    {
        Assert.Throws<NotSupportedException>(() => AlgoSelector.Resolve("nonexistent"));
    }

    [Fact]
    public void ListAll_ContainsAllAlgorithms()
    {
        var all = AlgoSelector.ListAll();
        Assert.True(all.Count >= 7);
        Assert.Contains(all, a => a.Kind == AlgorithmKind.SHA256);
        Assert.Contains(all, a => a.Kind == AlgorithmKind.RandomX);
    }

    [Fact]
    public void Default_IsSha256()
    {
        Assert.Equal(AlgorithmKind.SHA256, AlgoSelector.Default.Kind);
    }

    [Fact]
    public void HashWorker_InitialState()
    {
        using var w = new HashWorker(0);
        Assert.Equal(0, w.HashCount);
        Assert.Equal(0.0, w.Hashrate);
        Assert.Equal(0, w.ThreadIndex);
    }

    [Fact]
    public void AlgorithmInfo_Record_Properties()
    {
        var info = AlgoSelector.Resolve("ethash");
        Assert.Equal("Ethash", info.Name);
        Assert.Equal("ETH", info.CoinSymbol);
        Assert.Equal(12, info.BlockTime);
    }
}
