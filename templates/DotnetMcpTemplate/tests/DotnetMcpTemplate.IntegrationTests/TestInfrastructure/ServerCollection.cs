namespace DotnetMcpTemplate.IntegrationTests.TestInfrastructure;

/// <summary>
/// The app reads its settings from environment variables at startup, so servers with different settings
/// must not start in parallel. Every integration test class joins this collection.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ServerCollection
{
    public const string Name = "server";
}
