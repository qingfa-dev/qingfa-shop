using Testing;

namespace AppHost.Integration.Tests;

// Guard: every AppHost test starts the whole app, so each one must skip when the
// Vite dev servers cannot start.
internal static class JavaScriptResources
{
    public static void SkipUnlessToolchainAvailable()
    {
        if (!NodeToolchain.IsAvailable)
        {
            Assert.Skip(NodeToolchain.SkipReason);
        }
    }
}