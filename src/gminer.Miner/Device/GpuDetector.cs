using System.Runtime.InteropServices;

namespace Gminer.Miner.Device;

public sealed record GpuDevice(
    int Index,
    string Name,
    string Vendor,
    long MemoryBytes,
    string DriverVersion,
    bool IsAvailable
);

public static class GpuDetector
{
    public static List<GpuDevice> Detect()
    {
        var devices = new List<GpuDevice>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            DetectWindows(devices);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            DetectLinux(devices);

        return devices;
    }

    private static void DetectWindows(List<GpuDevice> devices)
    {
        var simulated = new (string Name, string Vendor, long Mem, string Driver)[]
        {
            ("NVIDIA GeForce RTX 4090",  "NVIDIA", 24L << 30, "560.94"),
            ("NVIDIA GeForce RTX 3080",  "NVIDIA", 10L << 30, "560.94"),
            ("AMD Radeon RX 7900 XTX",   "AMD",    24L << 30, "24.5.1"),
        };

        for (int i = 0; i < simulated.Length; i++)
        {
            var (name, vendor, mem, driver) = simulated[i];
            devices.Add(new GpuDevice(i, name, vendor, mem, driver, IsAvailable: true));
        }
    }

    private static void DetectLinux(List<GpuDevice> devices)
    {
        devices.Add(new GpuDevice(
            Index: 0,
            Name: "Generic GPU (sysfs)",
            Vendor: "Unknown",
            MemoryBytes: 8L << 30,
            DriverVersion: "unknown",
            IsAvailable: false
        ));
    }

    public static string FormatMemory(long bytes) => bytes switch
    {
        >= 1L << 30 => $"{bytes / (double)(1L << 30):F1} GB",
        >= 1L << 20 => $"{bytes / (double)(1L << 20):F1} MB",
        _           => $"{bytes} B",
    };
}
