# Redis

Redis is optional. You need it when you run **several instances** (they must share sign-ins: `AUTH_STORE=redis`), or
when your own code wants a shared cache, locks or rate limits. The app has **one Redis connection**
(StackExchange.Redis), used by the auth store and by your code alike, and **one place to decide how it connects**:
`IRedisConnectionFactory`.

```text
Core/Redis/
├── IRedisConnectionFactory.cs   the extension point + DefaultRedisConnectionFactory (REDIS_URL)
├── RedisConnectionString.cs     REDIS_URL → ConfigurationOptions (reuse it in your factory)
├── RedisConnection.cs           the shared connection, created on first use, retried after a failed connect
├── RedisHealthCheck.cs          "redis" in GET /health
└── RedisSetup.cs                AddAppRedis(), AddRedisConnectionFactory<T>()
```

## With a connection string: nothing to write

```bash
AUTH_STORE=redis
REDIS_URL=rediss://default:password@my-cache.example.com:6380/0
```

`REDIS_URL` accepts either format:

| Format | Example |
| --- | --- |
| URL (what most vendors hand out) | `redis://localhost:6379`, `redis://:password@host:6379/0`, `rediss://user:password@host:6380` (`rediss` = TLS) |
| StackExchange.Redis connection string | `host:6379,password=...,ssl=true,abortConnect=false` |
| Cluster | `node1:6379,node2:6379,node3:6379,password=...` (cluster mode is detected automatically) |
| Sentinel | `sentinel1:26379,sentinel2:26379,serviceName=mymaster` |

Escape special characters in URL passwords (`@` → `%40`, `%` → `%25`). The connection doesn't fail at startup while
Redis is unreachable; it keeps retrying, and `GET /health` reports `redis` as unhealthy (HTTP 503) until it's back.
All options of the connection string format: [StackExchange.Redis configuration](https://stackexchange.github.io/StackExchange.Redis/Configuration).

## With code: your own factory

When connecting needs code (tokens instead of passwords, secrets from a vault, custom certificates), implement
`IRedisConnectionFactory` and register it. Nothing else changes: the auth store, the health check and your code all
use the connection your factory returns.

```csharp
// Integrations/Redis/MyRedisConnectionFactory.cs
public sealed class MyRedisConnectionFactory(RedisSettings settings) : IRedisConnectionFactory
{
    public async Task<IConnectionMultiplexer> ConnectAsync(CancellationToken cancellationToken)
    {
        var options = RedisConnectionString.Parse(settings.Url!);   // start from REDIS_URL (or build from scratch)
        // ... vendor-specific setup ...
        return await ConnectionMultiplexer.ConnectAsync(options);
    }
}
```

```csharp
// Integrations/IntegrationsSetup.cs
services.AddRedisConnectionFactory<MyRedisConnectionFactory>();
```

Inject what you need (settings classes, `ILogger<T>`, SDK clients); the factory is a singleton and is called once,
or again only after a failed attempt. Add the vendor's NuGet package to `Directory.Packages.props`.

### Examples

The vendor calls below are sketches: check each SDK's docs for the version you use.

**Azure Cache for Redis / Azure Managed Redis with Microsoft Entra ID** (no password; the token is refreshed by the
package), with `Microsoft.Azure.StackExchangeRedis` and `Azure.Identity`:

```csharp
var options = RedisConnectionString.Parse(settings.Url!);            // e.g. rediss://my-cache.redis.cache.windows.net:6380
await options.ConfigureForAzureWithTokenCredentialAsync(new DefaultAzureCredential());
return await ConnectionMultiplexer.ConnectAsync(options);
```

**AWS ElastiCache / MemoryDB with an RBAC user from Secrets Manager**, with `AWSSDK.SecretsManager`:

```csharp
var secret = await secrets.GetSecretValueAsync(new GetSecretValueRequest { SecretId = "prod/redis" }, cancellationToken);
var credentials = JsonSerializer.Deserialize<RedisCredentials>(secret.SecretString)!;   // { "user": ..., "password": ... }

var options = RedisConnectionString.Parse(settings.Url!);            // rediss://my-cluster.xxxxxx.cache.amazonaws.com:6379
options.User = credentials.User;
options.Password = credentials.Password;
return await ConnectionMultiplexer.ConnectAsync(options);
```

IAM authentication works the same way with a generated token as the password; tokens are short-lived, so also
re-create the connection before they expire.

**Google Cloud Memorystore with in-transit encryption** (AUTH string + the instance's CA certificate):

```csharp
var options = RedisConnectionString.Parse(settings.Url!);            // rediss://:auth-string@10.0.0.3:6378
options.TrustIssuer("/secrets/memorystore-server-ca.pem");
return await ConnectionMultiplexer.ConnectAsync(options);
```

**Self-hosted with client certificates (mutual TLS):**

```csharp
var options = RedisConnectionString.Parse(settings.Url!);
options.CertificateSelection += (_, _, _, _, _) => X509Certificate2.CreateFromPemFile("/certs/client.pem", "/certs/client.key");
options.TrustIssuer("/certs/ca.pem");
return await ConnectionMultiplexer.ConnectAsync(options);
```

Settings your factory needs (secret ids, certificate paths) go in a settings class like any other; see
[configuration](configuration-and-environments.md#typed-settings).

## Using Redis in your code

```csharp
// Integrations/IntegrationsSetup.cs: only needed when AUTH_STORE isn't redis
services.AddAppRedis(configuration);
```

```csharp
public sealed class QuoteCache(RedisConnection redis)
{
    public async Task<string?> GetAsync(string symbol)
    {
        var db = await redis.GetDatabaseAsync();
        return await db.StringGetAsync($"quote:{symbol}");
    }
}
```

Inject `RedisConnection` in async code, or `IConnectionMultiplexer` directly. Use a key prefix per feature;
the auth store uses `mcp-auth:`.

## Operations

- **Several instances** need `AUTH_STORE=redis` *and* the same `AUTH_TOKEN_SIGNING_KEY` on every instance.
- **Persistence:** sign-ins live in Redis. Use a Redis with persistence or replication, or everyone signs in again
  after a Redis restart.
- **Health:** `GET /health` includes `redis` whenever Redis is registered. Point your load balancer or orchestrator at it.
- **Security:** use TLS (`rediss://`, `ssl=true`) and a password, ACL user or token outside local development.
