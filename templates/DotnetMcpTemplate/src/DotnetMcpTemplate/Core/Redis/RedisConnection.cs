using StackExchange.Redis;

namespace DotnetMcpTemplate.Core.Redis;

/// <summary>
/// The app's single Redis connection (StackExchange.Redis connections are meant to be shared). Created on first use
/// by <see cref="IRedisConnectionFactory"/>; if that throws, the next call tries again instead of failing forever.
/// Inject this in async code (<c>await redis.GetDatabaseAsync()</c>), or <see cref="IConnectionMultiplexer"/> directly.
/// </summary>
public sealed class RedisConnection(IRedisConnectionFactory factory) : IAsyncDisposable
{
    private readonly Lock _lock = new();
    private Task<IConnectionMultiplexer>? _connection;

    public Task<IConnectionMultiplexer> GetAsync()
    {
        lock (_lock)
        {
            if (_connection is null || _connection.IsFaulted || _connection.IsCanceled)
            {
                _connection = factory.ConnectAsync(CancellationToken.None);
            }

            return _connection;
        }
    }

    public async Task<IDatabase> GetDatabaseAsync(int database = -1) => (await GetAsync()).GetDatabase(database);

    public async ValueTask DisposeAsync()
    {
        Task<IConnectionMultiplexer>? connection;
        lock (_lock)
        {
            connection = _connection;
        }

        if (connection is { IsCompletedSuccessfully: true })
        {
            await connection.Result.DisposeAsync();
        }
    }
}
