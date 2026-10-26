// Assembly marker for Core.Unit.Tests
namespace Core.Unit.Tests;

/// <summary>
/// Marker type used to anchor the Core.Unit.Tests assembly and verify
/// project structure references at compile time.
/// </summary>
public sealed class AssemblyMarker
{
    /// <summary>Ensures the Core layer assembly is referenced.</summary>
    public static readonly System.Reflection.Assembly CoreAssembly = typeof(Core.AssemblyMarker).Assembly;
}
