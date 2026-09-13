using System.Collections.Concurrent;

namespace Gminer.Miner.Stratum;

public sealed record ShareResult(string JobId, string Nonce, bool Accepted, DateTimeOffset Timestamp);

public sealed class ShareSubmitter
{
    private readonly StratumClient _client;
    private readonly ConcurrentQueue<(string JobId, string Nonce, string Hash)> _queue = new();
    private long _accepted;
    private long _rejected;
    private long _total;

    public long Accepted => Interlocked.Read(ref _accepted);
    public long Rejected => Interlocked.Read(ref _rejected);
    public long Total => Interlocked.Read(ref _total);
    public double AcceptRate => _total > 0 ? (double)_accepted / _total * 100.0 : 0.0;

    public event Action<ShareResult>? OnShareProcessed;

    public ShareSubmitter(StratumClient client)
    {
        _client = client;
        _client.OnShareResult += HandlePoolResponse;
    }

    public void Enqueue(string jobId, string nonce, string hash) =>
        _queue.Enqueue((jobId, nonce, hash));

    public async Task RunAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var share))
            {
                try
                {
                    await _client.SubmitShareAsync(share.JobId, share.Nonce, share.Hash, ct);
                    Interlocked.Increment(ref _total);
                    OnShareProcessed?.Invoke(new ShareResult(share.JobId, share.Nonce, true, DateTimeOffset.UtcNow));
                }
                catch (Exception) when (!ct.IsCancellationRequested)
                {
                    _queue.Enqueue(share);
                    await Task.Delay(1000, ct);
                }
            }
            else
            {
                await Task.Delay(50, ct);
            }
        }
    }

    private void HandlePoolResponse(bool accepted)
    {
        if (accepted)
            Interlocked.Increment(ref _accepted);
        else
            Interlocked.Increment(ref _rejected);
    }
}
