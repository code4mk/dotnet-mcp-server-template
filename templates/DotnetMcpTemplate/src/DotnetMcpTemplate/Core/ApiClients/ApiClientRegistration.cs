using DotnetMcpTemplate.Core.ApiClients.Handlers;
using DotnetMcpTemplate.Core.Auth;
using DotnetMcpTemplate.Core.Common.Settings;
using Microsoft.Extensions.Http.Resilience;

namespace DotnetMcpTemplate.Core.ApiClients;

public static class ApiClientRegistration
{
    /// <summary>
    /// Registers a typed API client for one data source configured with environment variables {prefix}_*.
    /// Settings are validated immediately (startup fails with the variable names). Adds, in order:
    /// retries + circuit breaker + timeouts, authentication, correlation id and default headers.
    /// </summary>
    public static IHttpClientBuilder AddApiClient<TClient, TImplementation>(this IServiceCollection services, IConfiguration configuration, string prefix)
        where TClient : class
        where TImplementation : ApiClient, TClient
    {
        var settings = ReadSettings(configuration, prefix);
        services.AddKeyedSingleton(prefix, settings);

        var builder = services.AddHttpClient<TClient, TImplementation>(client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl.EndsWith('/') ? settings.BaseUrl : settings.BaseUrl + "/");
            client.Timeout = Timeout.InfiniteTimeSpan;   // timeouts are handled by the resilience pipeline
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"DotnetMcpTemplate/{Core.Mcp.AppVersion.Current}");
            foreach (var (name, value) in settings.DefaultHeaders)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
            }
        });

        // Outermost first: a retry re-runs the auth handler (fresh token) and keeps the correlation id.
        builder.AddStandardResilienceHandler(options =>
        {
            var attempt = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            options.AttemptTimeout.Timeout = attempt;
            options.TotalRequestTimeout.Timeout = attempt * (settings.RetryCount + 1) + TimeSpan.FromSeconds(settings.RetryCount * 5);
            options.CircuitBreaker.SamplingDuration = attempt * 2 > TimeSpan.FromSeconds(30) ? attempt * 2 : TimeSpan.FromSeconds(30);
            options.Retry.MaxRetryAttempts = settings.RetryCount;
            if (!settings.RetryUnsafeMethods)
            {
                options.Retry.DisableForUnsafeHttpMethods();
            }
        });

        switch (settings.Auth)
        {
            case ApiAuthTypes.Bearer:
                builder.AddHttpMessageHandler(() => new StaticHeaderHandler("Authorization", "Bearer " + settings.Token));
                break;
            case ApiAuthTypes.ApiKey:
                builder.AddHttpMessageHandler(() => new StaticHeaderHandler(settings.ApiKeyHeader, settings.ApiKey!));
                break;
            case ApiAuthTypes.Basic:
                builder.AddHttpMessageHandler(() => StaticHeaderHandler.Basic(settings.Username!, settings.Password ?? string.Empty));
                break;
            case ApiAuthTypes.ClientCredentials:
                services.AddKeyedSingleton(prefix, (sp, _) => new ClientCredentialsTokenProvider(
                    settings, sp.GetRequiredService<IHttpClientFactory>(), sp.GetRequiredService<TimeProvider>()));
                builder.AddHttpMessageHandler(sp => new ClientCredentialsHandler(sp.GetRequiredKeyedService<ClientCredentialsTokenProvider>(prefix)));
                break;
            case ApiAuthTypes.User:
                builder.AddHttpMessageHandler(sp => new UserTokenHandler(sp.GetRequiredService<IUpstreamTokenAccessor>(), prefix));
                break;
        }

        builder.AddHttpMessageHandler(sp => new CorrelationIdHandler(sp.GetRequiredService<IHttpContextAccessor>()));
        return builder;
    }

    public static ApiClientSettings ReadSettings(IConfiguration configuration, string prefix)
    {
        // Bind {prefix}_BASE_URL etc.: ConfigurationKeyName values start with "_", so bind against a prefixed view.
        var settings = new ApiClientSettings();
        new PrefixedConfiguration(configuration, prefix).Bind(settings);
        settings.Prefix = prefix;

        var errors = EnvSettingsValidation.Validate(settings, prefix);
        return errors.Count == 0
            ? settings
            : throw new InvalidOperationException($"Invalid settings for API client {prefix}: {string.Join("; ", errors)}");
    }

    /// <summary>Exposes {prefix}_NAME variables as _NAME for binding.</summary>
    private sealed class PrefixedConfiguration(IConfiguration inner, string prefix)
    {
        public void Bind(ApiClientSettings target)
        {
            var values = inner.AsEnumerable()
                .Where(pair => pair.Key.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase) && !pair.Key.Contains(':'))
                .ToDictionary(pair => pair.Key[prefix.Length..], pair => pair.Value, StringComparer.OrdinalIgnoreCase);

            new ConfigurationBuilder().AddInMemoryCollection(values).Build().Bind(target);
        }
    }
}
