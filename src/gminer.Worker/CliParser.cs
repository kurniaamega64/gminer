namespace Gminer.Worker;

public sealed class MinerOptions
{
    public string? PoolHost { get; set; }
    public int? PoolPort { get; set; }
    public string? Wallet { get; set; }
    public string? WorkerName { get; set; }
    public string? Algorithm { get; set; }
    public int? Threads { get; set; }
    public double DonatePercent { get; set; } = 1.0;
    public bool ShowHelp { get; set; }
    public bool ListAlgorithms { get; set; }
    public string? ConfigPath { get; set; }
}

public static class CliParser
{
    public static MinerOptions Parse(string[] args)
    {
        var o = new MinerOptions();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o" or "--url":
                    ParsePoolUrl(args[++i], o);
                    break;
                case "-u" or "--wallet":
                    o.Wallet = args[++i];
                    break;
                case "-w" or "--worker":
                    o.WorkerName = args[++i];
                    break;
                case "-a" or "--algo":
                    o.Algorithm = args[++i];
                    break;
                case "-t" or "--threads":
                    o.Threads = int.Parse(args[++i]);
                    break;
                case "--donate":
                    o.DonatePercent = double.Parse(args[++i]);
                    break;
                case "-c" or "--config":
                    o.ConfigPath = args[++i];
                    break;
                case "--algorithms":
                    o.ListAlgorithms = true;
                    break;
                case "-h" or "--help":
                    o.ShowHelp = true;
                    break;
            }
        }
        return o;
    }

    private static void ParsePoolUrl(string url, MinerOptions o)
    {
        var clean = url
            .Replace("stratum+tcp://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("stratum+ssl://", "", StringComparison.OrdinalIgnoreCase);
        var parts = clean.Split(':');
        o.PoolHost = parts[0];
        if (parts.Length > 1 && int.TryParse(parts[1], out var port))
            o.PoolPort = port;
    }

    public static void PrintUsage()
    {
        Console.WriteLine("""
        Usage: miner [options]

        Options:
          -o, --url <host:port>     Pool URL (stratum+tcp://host:port)
          -u, --wallet <address>    Wallet address
          -w, --worker <name>       Worker name (default: hostname)
          -a, --algo <name>         Algorithm (sha256, randomx, ethash, ...)
          -t, --threads <n>         CPU mining threads (default: cores - 1)
          --donate <percent>        Dev-fee percent (default: 1.0)
          -c, --config <path>       Config file (JSON)
          --algorithms              List supported algorithms
          -h, --help                Show this help
        """);
    }
}
