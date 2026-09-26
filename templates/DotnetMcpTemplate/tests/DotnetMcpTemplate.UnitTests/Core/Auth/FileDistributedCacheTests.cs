using System.Text;
using DotnetMcpTemplate.Core.Auth.Oidc;
using Microsoft.Extensions.Caching.Distributed;

namespace DotnetMcpTemplate.UnitTests.Core.Auth;

public sealed class FileDistributedCacheTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "mcp-auth-tests-" + Guid.NewGuid().ToString("N"));
    private readonly Clock _clock = new();

    private static DistributedCacheEntryOptions For(TimeSpan lifetime) => new() { AbsoluteExpirationRelativeToNow = lifetime };

    [Fact]
    public void Values_survive_a_restart()
    {
        new FileDistributedCache(_directory, _clock).Set("session:1", Encoding.UTF8.GetBytes("alice"), For(TimeSpan.FromDays(30)));

        var afterRestart = new FileDistributedCache(_directory, _clock);

        Assert.Equal("alice", Encoding.UTF8.GetString(afterRestart.Get("session:1")!));
    }

    [Fact]
    public void Expired_values_are_gone()
    {
        var cache = new FileDistributedCache(_directory, _clock);
        cache.Set("refresh:x", [1, 2, 3], For(TimeSpan.FromMinutes(5)));

        _clock.Now += TimeSpan.FromMinutes(6);

        Assert.Null(cache.Get("refresh:x"));
        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public void Set_replaces_and_remove_deletes()
    {
        var cache = new FileDistributedCache(_directory, _clock);
        cache.Set("client:a", [1], For(TimeSpan.FromDays(1)));
        cache.Set("client:a", [2], For(TimeSpan.FromDays(1)));

        Assert.Equal([2], cache.Get("client:a"));

        cache.Remove("client:a");
        Assert.Null(cache.Get("client:a"));
    }

    [Fact]
    public void Startup_sweeps_expired_entries()
    {
        new FileDistributedCache(_directory, _clock).Set("code:old", [1], For(TimeSpan.FromMinutes(5)));
        _clock.Now += TimeSpan.FromHours(1);

        _ = new FileDistributedCache(_directory, _clock);

        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public void Keys_are_not_readable_from_file_names()
    {
        new FileDistributedCache(_directory, _clock).Set("session:alice", [1], For(TimeSpan.FromDays(1)));

        Assert.DoesNotContain(Directory.GetFiles(_directory), f => f.Contains("alice", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
