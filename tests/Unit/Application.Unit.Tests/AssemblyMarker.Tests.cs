using System.Reflection;

namespace Application.Unit.Tests;

/// <summary>
/// Verifies that all project structure references for Application.Unit.Tests resolve correctly.
/// </summary>
public sealed class AssemblyMarkerTests
{
    private static readonly Assembly ApplicationAssembly = typeof(global::Application.AssemblyMarker).Assembly;
    private static readonly Assembly ContractAssembly = typeof(global::Contract.AssemblyMarker).Assembly;
    private static readonly Assembly CoreAssembly = typeof(global::Core.AssemblyMarker).Assembly;

    [Fact]
    public void Application_AssemblyExists()
    {
        ApplicationAssembly.Should().NotBeNull(because: "Application layer must be referenced");
    }

    [Fact]
    public void Contract_AssemblyExists()
    {
        ContractAssembly.Should().NotBeNull(because: "Contract layer must be referenced");
    }

    [Fact]
    public void Core_AssemblyExists()
    {
        CoreAssembly.Should().NotBeNull(because: "Core layer must be referenced");
    }
}
