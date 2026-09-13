using Gminer.Miner.Core;
using Gminer.Miner.Algo;

namespace Gminer.Worker;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine(Banner);

        var opts = CliParser.Parse(args);

        if (opts.ShowHelp)
        {
            CliParser.PrintUsage();
            return 0;
        }

        if (opts.ListAlgorithms)
        {
            Console.WriteLine("Supported algorithms:");
            foreach (var a in AlgoSelector.ListAll())
                Console.WriteLine($"  {a.Name,-15} {a.CoinSymbol,-6} block={a.BlockTime}s");
            return 0;
        }

        var pool = new PoolConfig
        {
            Host = opts.PoolHost ?? "pool.example.com",
            Port = opts.PoolPort ?? 3333,
            Wallet = opts.Wallet ?? throw new ArgumentException("--wallet is required"),
            Worker = opts.WorkerName ?? Environment.MachineName,
            Algorithm = opts.Algorithm ?? "sha256",
            DonatePercent = opts.DonatePercent,
        };
        pool.Validate();

        var algo = AlgoSelector.Resolve(pool.Algorithm);

        Console.WriteLine($"[Config] Pool    : {pool}");
        Console.WriteLine($"[Config] Algo    : {algo.Name} ({algo.CoinSymbol})");
        Console.WriteLine($"[Config] Threads : {opts.Threads ?? Environment.ProcessorCount - 1}");

        await using var ctl = new MineController(pool, algo, opts.Threads);

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n[Shutdown] Ctrl+C — stopping miner...");
            ctl.StopAsync().GetAwaiter().GetResult();
        };

        var wd = new Watchdog(ct => ctl.StartAsync(ct), maxRestarts: 5);
        wd.OnLog += Console.WriteLine;

        await wd.RunAsync();
        return 0;
    }

    private const string Banner =
        """

         ███╗   ███╗██╗███╗   ██╗███████╗██████╗
         ████╗ ████║██║████╗  ██║██╔════╝██╔══██╗
         ██╔████╔██║██║██╔██╗ ██║█████╗  ██████╔╝
         ██║╚██╔╝██║██║██║╚██╗██║██╔══╝  ██╔══██╗
         ██║ ╚═╝ ██║██║██║ ╚████║███████╗██║  ██║
         ╚═╝     ╚═╝╚═╝╚═╝  ╚═══╝╚══════╝╚═╝  ╚═╝

        """;
}
