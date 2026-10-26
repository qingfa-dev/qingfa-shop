using System.Reflection;

namespace Contract.Unit.Tests;

/// <summary>
/// Verifies that all project structure references for Contract.Unit.Tests resolve correctly.
/// </summary>
public sealed class AssemblyMarkerTests
{
    private static readonly Assembly ContractAssembly = typeof(Contract.AssemblyMarker).Assembly;

    [Fact]
    public void Contract_AssemblyExists()
    {
        ContractAssembly.Should().NotBeNull(because: "Contract layer must be referenced");
    }
}
