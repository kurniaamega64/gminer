using Xunit;
using System.Text.Json;
using Gminer.Miner.Stratum;

namespace Gminer.Miner.Tests;

public class StratumTests
{
    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement;

    [Fact]
    public void JobManager_ParseNotification_CreatesJob()
    {
        var mgr = new JobManager();
        var el = Parse("""
            {"method":"mining.notify","params":["job01","aabb","00112233","00000000ffffffff",false]}
        """);

        var job = mgr.ParseNotification(el);

        Assert.Equal("job01", job.JobId);
        Assert.Equal("aabb", job.PrevHash);
        Assert.NotEmpty(job.Blob);
        Assert.False(job.CleanJobs);
        Assert.Equal(job, mgr.CurrentJob);
    }

    [Fact]
    public void JobManager_CleanJobs_RemovesOld()
    {
        var mgr = new JobManager();
        var j1 = Parse("""{"method":"mining.notify","params":["old","aa","0011","ffffffffffffffff",false]}""");
        var j2 = Parse("""{"method":"mining.notify","params":["new","bb","2233","ffffffffffffffff",true]}""");

        mgr.ParseNotification(j1);
        mgr.ParseNotification(j2);

        Assert.Null(mgr.GetJob("old"));
        Assert.NotNull(mgr.GetJob("new"));
    }

    [Fact]
    public void JobManager_TracksReceivedCount()
    {
        var mgr = new JobManager();
        var el = Parse("""{"method":"mining.notify","params":["j","a","00","ffffffffffffffff",false]}""");

        mgr.ParseNotification(el);
        mgr.ParseNotification(el);

        Assert.Equal(2, mgr.TotalJobsReceived);
    }

    [Fact]
    public void JobManager_GetJob_ReturnsNullForMissing()
    {
        var mgr = new JobManager();
        Assert.Null(mgr.GetJob("nonexistent"));
    }

    [Fact]
    public void ShareResult_Record_Equality()
    {
        var a = new ShareResult("j1", "00000001", true, DateTimeOffset.UtcNow);
        var b = a with { Accepted = false };
        Assert.NotEqual(a, b);
        Assert.Equal(a.JobId, b.JobId);
    }
}
