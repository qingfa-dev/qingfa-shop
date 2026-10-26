// Assembly marker for Infrastructure.Integration.Tests
namespace Infrastructure.Integration.Tests;

/// <summary>
/// Marker type used to anchor the Infrastructure.Integration.Tests assembly and verify
/// project structure references at compile time.
/// </summary>
public sealed class AssemblyMarker
{
    /// <summary>Ensures the Infrastructure layer assembly is referenced.</summary>
    public static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(global::Infrastructure.AssemblyMarker).Assembly;

    /// <summary>Ensures the Core layer assembly is referenced.</summary>
    public static readonly System.Reflection.Assembly CoreAssembly = typeof(global::Core.AssemblyMarker).Assembly;
}
