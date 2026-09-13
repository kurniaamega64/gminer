namespace Gminer.Miner.Core;

public sealed class Watchdog : IDisposable
{
    private readonly Func<CancellationToken, Task> _target;
    private readonly TimeSpan _interval;
    private readonly int _maxRestarts;
    private CancellationTokenSource? _cts;
    private int _restarts;
    private DateTimeOffset _lastBeat;
    private volatile bool _disposed;

    public int RestartCount => _restarts;
    public bool IsHealthy => DateTimeOffset.UtcNow - _lastBeat < _interval * 3;

    public event Action<int, Exception?>? OnRestart;
    public event Action<string>? OnLog;

    public Watchdog(Func<CancellationToken, Task> target,
                    TimeSpan? interval = null,
                    int maxRestarts = 10)
    {
        _target = target;
        _interval = interval ?? TimeSpan.FromSeconds(30);
        _maxRestarts = maxRestarts;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        while (!_cts.IsCancellationRequested && _restarts < _maxRestarts)
        {
            _lastBeat = DateTimeOffset.UtcNow;
            try
            {
                OnLog?.Invoke($"[Watchdog] Attempt {_restarts + 1}/{_maxRestarts}");
                var beat = HeartbeatAsync(_cts.Token);
                var work = _target(_cts.Token);
                await Task.WhenAny(work, beat);

                if (work.IsCompletedSuccessfully)
                {
                    OnLog?.Invoke("[Watchdog] Target finished normally");
                    break;
                }
                if (work.IsFaulted) throw work.Exception!.InnerException!;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _restarts++;
                OnRestart?.Invoke(_restarts, ex);
                OnLog?.Invoke($"[Watchdog] Crash #{_restarts}: {ex.Message}");
                var backoff = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, _restarts)));
                OnLog?.Invoke($"[Watchdog] Restart in {backoff.TotalSeconds:F0}s");
                await Task.Delay(backoff, ct);
            }
        }

        if (_restarts >= _maxRestarts)
            OnLog?.Invoke($"[Watchdog] Exhausted {_maxRestarts} restarts — aborting");
    }

    private async Task HeartbeatAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            _lastBeat = DateTimeOffset.UtcNow;
            await Task.Delay(_interval, ct);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
