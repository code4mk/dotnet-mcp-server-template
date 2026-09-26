using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>
/// AUTH_STORE=file: the OAuth proxy's state (client registrations, sessions, refresh tokens) in files, so sign-ins
/// survive restarts and deploys on a single instance. One file per key (file name = SHA-256 of the key), holding the
/// expiry and the value; writes go to a temp file and are moved into place, so a crash never leaves half a file.
/// Values are what <see cref="AuthStore"/> writes: IdP tokens inside are already encrypted. For several instances use
/// AUTH_STORE=redis.
/// </summary>
public sealed class FileDistributedCache : IDistributedCache
{
    private const int HeaderSize = sizeof(long);
    private const int SweepEveryWrites = 200;

    private readonly string _directory;
    private readonly TimeProvider _time;
    private int _writes;

    public FileDistributedCache(string directory, TimeProvider time)
    {
        _directory = Path.GetFullPath(directory);
        _time = time;

        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(_directory);
        }
        else
        {
            Directory.CreateDirectory(_directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        Sweep();
    }

    public byte[]? Get(string key)
    {
        var path = PathOf(key);
        byte[] content;
        try
        {
            content = File.ReadAllBytes(path);
        }
        catch (FileNotFoundException)
        {
            return null;
        }

        if (content.Length < HeaderSize || IsExpired(content))
        {
            TryDelete(path);
            return null;
        }

        return content[HeaderSize..];
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var expires = ExpiryOf(options);
        var content = new byte[HeaderSize + value.Length];
        BinaryPrimitives.WriteInt64LittleEndian(content, expires.UtcTicks);
        value.CopyTo(content, HeaderSize);

        var path = PathOf(key);
        var temp = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllBytes(temp, content);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(temp, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        File.Move(temp, path, overwrite: true);

        if (Interlocked.Increment(ref _writes) % SweepEveryWrites == 0)
        {
            Sweep();
        }
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key)
    {
        // Sliding expiration isn't used: AuthStore sets absolute lifetimes and re-sets entries to extend them.
    }

    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    public void Remove(string key) => TryDelete(PathOf(key));

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    /// <summary>Deletes expired entries and temp files left by a crash.</summary>
    private void Sweep()
    {
        Span<byte> header = stackalloc byte[HeaderSize];
        foreach (var path in Directory.EnumerateFiles(_directory))
        {
            if (path.EndsWith(".tmp", StringComparison.Ordinal))
            {
                if (File.GetLastWriteTimeUtc(path) < _time.GetUtcNow().UtcDateTime.AddMinutes(-5))
                {
                    TryDelete(path);
                }

                continue;
            }

            try
            {
                using var stream = File.OpenRead(path);
                if (stream.Read(header) < HeaderSize || IsExpired(header))
                {
                    stream.Dispose();
                    TryDelete(path);
                }
            }
            catch (IOException)
            {
                // Being written or deleted concurrently: the next sweep gets it.
            }
        }
    }

    private bool IsExpired(ReadOnlySpan<byte> content) =>
        BinaryPrimitives.ReadInt64LittleEndian(content) <= _time.GetUtcNow().UtcTicks;

    private DateTimeOffset ExpiryOf(DistributedCacheEntryOptions options)
    {
        var now = _time.GetUtcNow();
        if (options.AbsoluteExpiration is { } absolute)
        {
            return absolute;
        }

        var relative = options.AbsoluteExpirationRelativeToNow ?? options.SlidingExpiration;
        return relative is { } lifetime ? now + lifetime : DateTimeOffset.MaxValue;
    }

    private string PathOf(string key) =>
        Path.Combine(_directory, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key))) + ".bin");

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
    }
}
