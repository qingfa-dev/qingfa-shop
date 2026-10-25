namespace Contract.Tests;

/// <summary>
/// Placeholder suite for the Contract layer. The layer currently holds no behaviour to
/// exercise, so this guards the assembly identity that the architecture tests and the
/// Api composition rely on.
/// </summary>
public sealed class AssemblyMarkerTest
{
    [Fact]
    public void AssemblyName_MatchesProjectName()
    {
        typeof(AssemblyMarker).Assembly.GetName().Name.Should().Be("Contract");
    }
}