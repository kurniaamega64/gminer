using System.Text.Json;
using Gminer.Miner.Stratum;
using Gminer.Miner.Algo;
using Gminer.Miner.Device;

namespace Gminer.Miner.Core;

public sealed class MineController : IAsyncDisposable
{
    private readonly StratumClient _stratum;
    private readonly JobManager _jobMgr;
    private readonly ShareSubmitter _shares;
    private readonly CpuBackend _cpu;
    private readonly AlgorithmInfo _algo;
    private readonly PoolConfig _pool;
    private CancellationTokenSource _cts = new();

    public bool IsRunning { get; private set; }
    public double Hashrate => _cpu.TotalHashrate;
    public long ShareCount => _shares.Total;
    public double AcceptRate => _shares.AcceptRate;

    public MineController(PoolConfig pool, AlgorithmInfo algo, int? cpuThreads = null)
    {
        _pool = pool;
        _algo = algo;
        _stratum = new StratumClient(pool.Host, pool.Port);
        _jobMgr = new JobManager();
        _shares = new ShareSubmitter(_stratum);
        _cpu = new CpuBackend(cpuThreads);

        _stratum.OnJobReceived += OnJob;
        _cpu.OnShareFound += OnShare;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        IsRunning = true;

        await _stratum.ConnectAsync(_cts.Token);
        await _stratum.SubscribeAsync(_cts.Token);
        await _stratum.AuthorizeAsync(_pool.Wallet, _pool.Worker, _cts.Token);

        Console.WriteLine($"[{_algo.Name}] Mining started | {_cpu.ThreadCount} threads");
        Console.WriteLine($"[Pool] {_pool.Host}:{_pool.Port} | wallet={_pool.Wallet}");

        var listen = _stratum.ListenAsync(_cts.Token);
        var submit = _shares.RunAsync(_cts.Token);
        await Task.WhenAny(listen, submit);
    }

    public async Task StopAsync()
    {
        IsRunning = false;
        await _cts.CancelAsync();
        _cpu.Stop();
        await _stratum.DisposeAsync();
        Console.WriteLine(
            $"[Stop] hashes={_cpu.TotalHashes} shares={ShareCount} accept={AcceptRate:F1}%");
    }

    private void OnJob(JsonElement el)
    {
        var job = _jobMgr.ParseNotification(el);
        Console.WriteLine($"[Job] {job.JobId[..Math.Min(8, job.JobId.Length)]} target={job.Target:x16}");
        _cpu.Start(job.Blob, job.Target, job.JobId);
    }

    private void OnShare(string nonce, string hash)
    {
        var job = _jobMgr.CurrentJob;
        if (job is null) return;
        _shares.Enqueue(job.JobId, nonce, hash);
        Console.WriteLine($"[Share] nonce={nonce} hash={hash[..Math.Min(16, hash.Length)]}...");
    }

    public void PrintStatus() =>
        Console.WriteLine(
            $"[Status] {Hashrate:F2} H/s | shares={ShareCount} | accept={AcceptRate:F1}% | algo={_algo.Name}");

    public async ValueTask DisposeAsync()
    {
        if (IsRunning) await StopAsync();
        await _cpu.DisposeAsync();
        _cts.Dispose();
    }
}
