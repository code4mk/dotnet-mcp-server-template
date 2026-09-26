using DotnetMcpTemplate.Core.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace DotnetMcpTemplate.UnitTests.Core.Redis;

public sealed class RedisTests
{
    [Fact]
    public void Url_with_acl_user_tls_and_database()
    {
        var options = RedisConnectionString.Parse("rediss://app-user:p%40ss@cache.example.com:6380/2");

        Assert.Equal("cache.example.com:6380", options.EndPoints.Single().ToString()?.Replace("Unspecified/", ""));
        Assert.True(options.Ssl);
        Assert.Equal("cache.example.com", options.SslHost);
        Assert.Equal("app-user", options.User);
        Assert.Equal("p@ss", options.Password);
        Assert.Equal(2, options.DefaultDatabase);
        Assert.False(options.AbortOnConnectFail);
    }

    [Fact]
    public void Url_with_password_only_and_default_port()
    {
        var options = RedisConnectionString.Parse("redis://:secret@localhost");

        Assert.Equal("localhost:6379", options.EndPoints.Single().ToString()?.Replace("Unspecified/", ""));
        Assert.False(options.Ssl);
        Assert.Null(options.User);
        Assert.Equal("secret", options.Password);
    }

    [Fact]
    public void Connection_string_for_a_cluster_is_passed_through()
    {
        var options = RedisConnectionString.Parse("node1:6379,node2:6379,node3:6379,password=pw,ssl=true");

        Assert.Equal(3, options.EndPoints.Count);
        Assert.Equal("pw", options.Password);
        Assert.True(options.Ssl);
        Assert.False(options.AbortOnConnectFail);
    }

    [Fact]
    public void Explicit_abort_connect_is_kept()
    {
        Assert.True(RedisConnectionString.Parse("localhost:6379,abortConnect=true").AbortOnConnectFail);
    }

    [Fact]
    public void Sentinel_service_name_is_passed_through()
    {
        Assert.Equal("mymaster", RedisConnectionString.Parse("sentinel1:26379,serviceName=mymaster").ServiceName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_custom_factory_replaces_the_default_in_either_order(bool customFirst)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        if (customFirst)
        {
            services.AddRedisConnectionFactory<FakeFactory>();
            services.AddAppRedis(configuration);
        }
        else
        {
            services.AddAppRedis(configuration);
            services.AddRedisConnectionFactory<FakeFactory>();
        }

        using var provider = services.BuildServiceProvider();

        Assert.IsType<FakeFactory>(provider.GetRequiredService<IRedisConnectionFactory>());
        Assert.Single(services, d => d.ServiceType == typeof(IRedisConnectionFactory));
    }

    [Fact]
    public async Task A_failed_connect_is_retried_on_the_next_call()
    {
        var factory = new FlakyFactory();
        await using var connection = new RedisConnection(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(connection.GetAsync);
        await Assert.ThrowsAsync<InvalidOperationException>(connection.GetAsync);

        Assert.Equal(2, factory.Attempts);
    }

    [Fact]
    public async Task The_default_factory_explains_a_missing_url()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DefaultRedisConnectionFactory(new RedisSettings()).ConnectAsync(CancellationToken.None));

        Assert.Contains("REDIS_URL", error.Message);
        Assert.Contains("IRedisConnectionFactory", error.Message);
    }

    private sealed class FakeFactory : IRedisConnectionFactory
    {
        public Task<IConnectionMultiplexer> ConnectAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FlakyFactory : IRedisConnectionFactory
    {
        public int Attempts { get; private set; }

        public Task<IConnectionMultiplexer> ConnectAsync(CancellationToken cancellationToken)
        {
            Attempts++;
            return Task.FromException<IConnectionMultiplexer>(new InvalidOperationException("token endpoint down"));
        }
    }
}
