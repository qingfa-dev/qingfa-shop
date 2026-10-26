using System.Reflection;

namespace Infrastructure.Integration.Tests;

/// <summary>
/// Verifies that all project structure references for Infrastructure.Integration.Tests resolve correctly.
/// </summary>
public sealed class AssemblyMarkerTests
{
    private static readonly Assembly InfrastructureAssembly = typeof(global::Infrastructure.AssemblyMarker).Assembly;
    private static readonly Assembly CoreAssembly = typeof(global::Core.AssemblyMarker).Assembly;

    [Fact]
    public void Infrastructure_AssemblyExists()
    {
        InfrastructureAssembly.Should().NotBeNull(because: "Infrastructure layer must be referenced");
    }

    [Fact]
    public void Core_AssemblyExists()
    {
        CoreAssembly.Should().NotBeNull(because: "Core layer must be referenced");
    }
}
