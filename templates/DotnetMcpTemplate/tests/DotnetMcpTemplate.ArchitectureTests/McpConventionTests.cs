using System.Reflection;
using DotnetMcpTemplate.Core.Mcp;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.ArchitectureTests;

/// <summary>Rules that keep tools, resources and prompts secure and consistent. They fail the build, not code review.</summary>
public sealed class McpConventionTests
{
    private static readonly Assembly App = typeof(McpSettings).Assembly;

    private static IEnumerable<Type> McpTypes() => App.GetTypes().Where(t =>
        t.IsDefined(typeof(McpServerToolTypeAttribute)) || t.IsDefined(typeof(McpServerResourceTypeAttribute)) || t.IsDefined(typeof(McpServerPromptTypeAttribute)));

    /// <summary>API client plumbing (Core/ApiClients) and the concrete clients and their models (Integrations/).</summary>
    private static bool IsExternalApi(Type type) =>
        type.Namespace is { } ns
        && (ns.StartsWith("DotnetMcpTemplate.Core.ApiClients", StringComparison.Ordinal)
            || ns.StartsWith("DotnetMcpTemplate.Integrations", StringComparison.Ordinal));

    [Fact]
    public void Every_tool_resource_and_prompt_class_declares_its_auth()
    {
        var missing = McpTypes()
            .Where(t => !t.IsDefined(typeof(AuthorizeAttribute)) && !t.IsDefined(typeof(AllowAnonymousAttribute)))
            .Select(t => t.Name)
            .ToList();

        Assert.True(missing.Count == 0, $"Add [Authorize] or [AllowAnonymous] to: {string.Join(", ", missing)}");
    }

    [Fact]
    public void Every_tool_and_prompt_has_a_unique_explicit_name()
    {
        var registry = new McpHandlerRegistry();   // throws on missing or duplicate names
        Assert.NotEmpty(registry.Tools);
        Assert.NotEmpty(registry.Prompts);
    }

    [Fact]
    public void Every_resource_has_a_uri_template_and_name()
    {
        var bad = McpTypes()
            .SelectMany(t => t.GetMethods())
            .Select(m => (Method: m, Attribute: m.GetCustomAttribute<McpServerResourceAttribute>()))
            .Where(x => x.Attribute is not null && (string.IsNullOrWhiteSpace(x.Attribute.UriTemplate) || string.IsNullOrWhiteSpace(x.Attribute.Name)))
            .Select(x => x.Method.Name)
            .ToList();

        Assert.True(bad.Count == 0, $"Set UriTemplate and Name on: {string.Join(", ", bad)}");
    }

    [Fact]
    public void Tools_resources_and_prompts_use_services_not_api_clients()
    {
        var offenders = McpTypes()
            .SelectMany(t => t.GetConstructors().SelectMany(c => c.GetParameters())
                .Concat(t.GetMethods().SelectMany(m => m.GetParameters()))
                .Where(p => IsExternalApi(p.ParameterType))
                .Select(p => $"{t.Name} → {p.ParameterType.Name}"))
            .ToList();

        Assert.True(offenders.Count == 0, $"Call a service instead of an API client: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Services_know_nothing_about_mcp()
    {
        var offenders = App.GetTypes()
            .Where(t => t.Namespace == "DotnetMcpTemplate.Services")
            .SelectMany(t => t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(c => c.GetParameters())
                .Where(p => p.ParameterType.Namespace?.StartsWith("ModelContextProtocol", StringComparison.Ordinal) == true)
                .Select(p => $"{t.Name} → {p.ParameterType.Name}"))
            .ToList();

        Assert.True(offenders.Count == 0, $"Services must not depend on MCP types: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Service_interfaces_do_not_expose_external_api_models()
    {
        var offenders = App.GetTypes()
            .Where(t => t is { IsInterface: true, Namespace: "DotnetMcpTemplate.Services" })
            .SelectMany(t => t.GetMethods())
            .Where(m => Mentions(m.ReturnType) || m.GetParameters().Any(p => Mentions(p.ParameterType)))
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
            .ToList();

        Assert.True(offenders.Count == 0, $"Return DTOs from Models/, not API client models: {string.Join(", ", offenders)}");

        static bool Mentions(Type type) =>
            IsExternalApi(type)
            || (type.IsGenericType && type.GetGenericArguments().Any(Mentions));
    }
}
