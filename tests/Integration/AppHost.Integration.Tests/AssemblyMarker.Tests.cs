using System.Reflection;

namespace AppHost.Integration.Tests;

/// <summary>
/// Verifies that all project structure references for AppHost.Integration.Tests resolve correctly.
/// </summary>
public sealed class AssemblyMarkerTests
{
    private static readonly Assembly AppHostAssembly = typeof(Projects.AppHost).Assembly;

    [Fact]
    public void AppHost_AssemblyExists()
    {
        AppHostAssembly.Should().NotBeNull(because: "AppHost layer must be referenced");
    }
}
