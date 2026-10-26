using System.Reflection;

namespace Core.Unit.Tests;

/// <summary>
/// Verifies that all project structure references for Core.Unit.Tests resolve correctly.
/// </summary>
public sealed class AssemblyMarkerTests
{
    private static readonly Assembly CoreAssembly = typeof(AssemblyMarker).Assembly;

    [Fact]
    public void Core_AssemblyExists()
    {
        CoreAssembly.Should().NotBeNull(because: "Core layer must be referenced");
    }
}
