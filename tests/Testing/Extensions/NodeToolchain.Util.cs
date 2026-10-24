namespace Testing;

/// <summary>
/// Reports whether the Node/pnpm toolchain the Vite resources need is present,
/// so their tests can skip instead of waiting out the resource timeout.
/// </summary>
public static class NodeToolchain
{
    private static readonly Lazy<bool> Detected = new(Probe);

    /// <summary>True when both <c>node</c> and <c>pnpm</c> resolve on PATH.</summary>
    public static bool IsAvailable => Detected.Value;

    /// <summary>Reason handed to <c>Assert.Skip</c> when the toolchain is missing.</summary>
    public static string SkipReason =>
        "The Node/pnpm toolchain is not on PATH, so the Vite dev servers cannot start.";

    // Validate: the dev servers need both binaries; probing PATH here avoids a
    // multi-minute wait for a resource that can never reach Running.
    private static bool Probe() => Resolves("node") && Resolves("pnpm");

    private static bool Resolves(string executable)
    {
        var suffixes = OperatingSystem.IsWindows() ? new[] { ".exe", ".cmd", "" } : new[] { "" };
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var suffix in suffixes)
            {
                if (File.Exists(Path.Combine(directory, executable + suffix)))
                {
                    return true;
                }
            }
        }

        return false;
    }
}