using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace Gminer.Miner.Stratum;

public sealed record MiningJob
{
    public string JobId { get; init; } = string.Empty;
    public string PrevHash { get; init; } = string.Empty;
    public byte[] Blob { get; init; } = [];
    public ulong Target { get; init; }
    public long Height { get; init; }
    public bool CleanJobs { get; init; }
    public DateTimeOffset ReceivedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class JobManager
{
    private readonly ConcurrentDictionary<string, MiningJob> _jobs = new();
    private volatile MiningJob? _current;
    private long _totalReceived;

    public MiningJob? CurrentJob => _current;
    public long TotalJobsReceived => Interlocked.Read(ref _totalReceived);

    public event Action<MiningJob>? OnNewJob;

    public MiningJob ParseNotification(JsonElement element)
    {
        var p = element.GetProperty("params");
        var jobId = p[0].GetString() ?? throw new FormatException("Missing job_id");
        var prevHash = p[1].GetString() ?? string.Empty;
        var blobHex = p[2].GetString() ?? string.Empty;
        var targetHex = p[3].GetString() ?? "ffffffffffffffff";
        bool clean = p.GetArrayLength() > 4 && p[4].ValueKind == JsonValueKind.True;

        var job = new MiningJob
        {
            JobId = jobId,
            PrevHash = prevHash,
            Blob = Convert.FromHexString(blobHex),
            Target = ulong.Parse(targetHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            CleanJobs = clean,
            ReceivedAt = DateTimeOffset.UtcNow,
        };

        if (clean) _jobs.Clear();

        _jobs[jobId] = job;
        _current = job;
        Interlocked.Increment(ref _totalReceived);
        OnNewJob?.Invoke(job);
        return job;
    }

    public MiningJob? GetJob(string jobId) =>
        _jobs.TryGetValue(jobId, out var j) ? j : null;

    public void Expire(TimeSpan maxAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        foreach (var kv in _jobs)
        {
            if (kv.Value.ReceivedAt < cutoff)
                _jobs.TryRemove(kv.Key, out _);
        }
    }
}
