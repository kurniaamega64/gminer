using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Gminer.Miner.Stratum;

public sealed class StratumClient : IAsyncDisposable
{
    private TcpClient? _tcp;
    private NetworkStream? _stream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private int _messageId;
    private readonly string _host;
    private readonly int _port;

    public event Action<JsonElement>? OnJobReceived;
    public event Action<bool>? OnShareResult;
    public bool IsConnected => _tcp?.Connected ?? false;

    public StratumClient(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _tcp = new TcpClient();
        await _tcp.ConnectAsync(_host, _port, ct);
        _stream = _tcp.GetStream();
        _reader = new StreamReader(_stream, Encoding.UTF8);
        _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
    }

    public async Task<JsonElement> SubscribeAsync(CancellationToken ct = default)
    {
        var id = Interlocked.Increment(ref _messageId);
        var req = new { id, method = "mining.subscribe", @params = new object[] { "agent/1.0" } };
        await SendLineAsync(JsonSerializer.Serialize(req), ct);
        return await ReadResponseAsync(ct);
    }

    public async Task<bool> AuthorizeAsync(string wallet, string worker, CancellationToken ct = default)
    {
        var id = Interlocked.Increment(ref _messageId);
        var req = new { id, method = "mining.authorize", @params = new[] { $"{wallet}.{worker}", "x" } };
        await SendLineAsync(JsonSerializer.Serialize(req), ct);
        var resp = await ReadResponseAsync(ct);
        return resp.TryGetProperty("result", out var r) && r.ValueKind == JsonValueKind.True;
    }

    public async Task SubmitShareAsync(string jobId, string nonce, string hash, CancellationToken ct = default)
    {
        var id = Interlocked.Increment(ref _messageId);
        var req = new { id, method = "mining.submit", @params = new[] { jobId, nonce, hash } };
        await SendLineAsync(JsonSerializer.Serialize(req), ct);
        var resp = await ReadResponseAsync(ct);
        bool accepted = resp.TryGetProperty("result", out var r) && r.ValueKind == JsonValueKind.True;
        OnShareResult?.Invoke(accepted);
    }

    public async Task ListenAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested && _reader is not null)
        {
            var line = await _reader.ReadLineAsync(ct);
            if (line is null) break;
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.TryGetProperty("method", out var m) && m.GetString() == "mining.notify")
            {
                OnJobReceived?.Invoke(root.Clone());
            }
        }
    }

    private async Task SendLineAsync(string data, CancellationToken ct)
    {
        if (_writer is null) throw new InvalidOperationException("Not connected to pool");
        await _writer.WriteLineAsync(data.AsMemory(), ct);
    }

    private async Task<JsonElement> ReadResponseAsync(CancellationToken ct)
    {
        if (_reader is null) throw new InvalidOperationException("Not connected to pool");
        var line = await _reader.ReadLineAsync(ct) ?? throw new IOException("Pool connection closed");
        using var doc = JsonDocument.Parse(line);
        return doc.RootElement.Clone();
    }

    public async ValueTask DisposeAsync()
    {
        if (_writer is not null) await _writer.DisposeAsync();
        _reader?.Dispose();
        if (_stream is not null) await _stream.DisposeAsync();
        _tcp?.Dispose();
    }
}
