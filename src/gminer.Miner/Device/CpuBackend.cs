using System.Runtime.InteropServices;
using Gminer.Miner.Algo;

namespace Gminer.Miner.Device;

public sealed class CpuBackend : IAsyncDisposable
{
    private readonly int _threadCount;
    private readonly List<HashWorker> _workers = [];
    private readonly List<Task> _tasks = [];
    private CancellationTokenSource? _cts;

    public int ThreadCount => _threadCount;
    public bool IsRunning => _cts is not null && !_cts.IsCancellationRequested;
    public double TotalHashrate => _workers.Sum(w => w.Hashrate);
    public long TotalHashes => _workers.Sum(w => w.HashCount);

    public event Action<string, string>? OnShareFound;

    public CpuBackend(int? threads = null)
    {
        _threadCount = threads ?? Math.Max(1, Environment.ProcessorCount - 1);
    }

    public void Start(byte[] blob, ulong target, string jobId)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _workers.Clear();
        _tasks.Clear();

        for (int i = 0; i < _threadCount; i++)
        {
            var worker = new HashWorker(i);
            worker.OnShareFound += (nonce, hash) => OnShareFound?.Invoke(nonce, hash);
            _workers.Add(worker);
            _tasks.Add(worker.RunAsync(blob, target, jobId, _cts.Token));
        }
    }

    public void Stop()
    {
        if (_cts is null) return;
        _cts.Cancel();
        foreach (var w in _workers) w.Stop();
        _cts.Dispose();
        _cts = null;
    }

    public CpuInfo DetectCpu() => new(
        Description: RuntimeInformation.OSDescription,
        Architecture: RuntimeInformation.OSArchitecture.ToString(),
        LogicalCores: Environment.ProcessorCount,
        MiningThreads: _threadCount
    );

    public async ValueTask DisposeAsync()
    {
        Stop();
        if (_tasks.Count > 0)
        {
            try { await Task.WhenAll(_tasks); }
            catch (OperationCanceledException) { }
        }
        foreach (var w in _workers) w.Dispose();
        _workers.Clear();
        _tasks.Clear();
    }
}

public sealed record CpuInfo(string Description, string Architecture, int LogicalCores, int MiningThreads);
