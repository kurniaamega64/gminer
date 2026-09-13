using System.Diagnostics;
using System.Security.Cryptography;

namespace Gminer.Miner.Algo;

public sealed class HashWorker : IDisposable
{
    private readonly int _threadIndex;
    private readonly IncrementalHash _hasher;
    private long _hashCount;
    private readonly Stopwatch _sw = new();
    private volatile bool _running;

    public int ThreadIndex => _threadIndex;
    public long HashCount => Interlocked.Read(ref _hashCount);
    public double Hashrate => _sw.Elapsed.TotalSeconds > 0
        ? HashCount / _sw.Elapsed.TotalSeconds
        : 0;

    public event Action<string, string>? OnShareFound;

    public HashWorker(int threadIndex)
    {
        _threadIndex = threadIndex;
        _hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    }

    public async Task RunAsync(byte[] blob, ulong target, string jobId, CancellationToken ct = default)
    {
        _running = true;
        _sw.Restart();
        var nonce = (uint)(_threadIndex * 0x1000000);
        var buffer = new byte[blob.Length];

        await Task.Run(() =>
        {
            while (_running && !ct.IsCancellationRequested)
            {
                Buffer.BlockCopy(blob, 0, buffer, 0, blob.Length);
                if (buffer.Length >= 43)
                    BitConverter.TryWriteBytes(buffer.AsSpan(39), nonce);

                _hasher.AppendData(buffer);
                var hash = _hasher.GetHashAndReset();
                Interlocked.Increment(ref _hashCount);

                if (hash.Length >= 32)
                {
                    var result = BitConverter.ToUInt64(hash.AsSpan(24));
                    if (result < target)
                    {
                        var nonceHex = nonce.ToString("x8");
                        var hashHex = Convert.ToHexString(hash).ToLowerInvariant();
                        OnShareFound?.Invoke(nonceHex, hashHex);
                    }
                }

                nonce++;
                if (nonce % 0x10000 == 0 && ct.IsCancellationRequested) break;
            }
        }, ct);

        _sw.Stop();
        _running = false;
    }

    public void Stop() => _running = false;

    public void ResetCounters()
    {
        Interlocked.Exchange(ref _hashCount, 0);
        _sw.Reset();
    }

    public void Dispose()
    {
        _running = false;
        _hasher.Dispose();
    }
}
