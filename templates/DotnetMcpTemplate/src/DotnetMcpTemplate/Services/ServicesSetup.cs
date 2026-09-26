namespace DotnetMcpTemplate.Services;

public static class ServicesSetup
{
    /// <summary>Business services. Scoped: one instance per MCP request.</summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ISearchService, SearchService>();
        return services;
    }
}
