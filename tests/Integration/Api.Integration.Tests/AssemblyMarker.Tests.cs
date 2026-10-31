using System.Reflection;

namespace Api.Integration.Tests;

/// <summary>
/// Verifies that all project structure references for Api.Integration.Tests resolve correctly.
/// </summary>
public sealed class AssemblyMarkerTests
{
    private static readonly Assembly WebApiAssembly = typeof(global::Web.Api.AssemblyMarker).Assembly;

    [Fact]
    public void WebApi_AssemblyExists()
    {
        WebApiAssembly.Should().NotBeNull(because: "Web.Api layer must be referenced");
    }
}
