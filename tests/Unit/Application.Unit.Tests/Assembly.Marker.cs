// Assembly marker for Application.Unit.Tests
namespace Application.Unit.Tests;

/// <summary>
/// Marker type used to anchor the Application.Unit.Tests assembly and verify
/// project structure references at compile time.
/// </summary>
public sealed class AssemblyMarker
{
    /// <summary>Ensures the Application layer assembly is referenced.</summary>
    public static readonly System.Reflection.Assembly ApplicationAssembly = typeof(global::Application.AssemblyMarker).Assembly;

    /// <summary>Ensures the Core layer assembly is referenced.</summary>
    public static readonly System.Reflection.Assembly CoreAssembly = typeof(global::Core.AssemblyMarker).Assembly;
}
