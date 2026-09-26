namespace DotnetMcpTemplate.IntegrationTests.TestInfrastructure;

/// <summary>The real clock plus an offset tests can move forward, e.g. past a grace period.</summary>
public sealed class TestClock : TimeProvider
{
    public TimeSpan Offset { get; private set; }

    public void Advance(TimeSpan by) => Offset += by;

    public void Reset() => Offset = TimeSpan.Zero;

    public override DateTimeOffset GetUtcNow() => base.GetUtcNow() + Offset;
}
