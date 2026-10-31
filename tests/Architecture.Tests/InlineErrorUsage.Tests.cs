using System.Text.RegularExpressions;

namespace QingFa.Shop.Architecture.Tests;

/// <summary>
/// Enforces that error construction lives in outcome classes.
/// </summary>
/// <remarks>
/// Every error factory belongs in an <c>*Outcome.cs</c> file so handlers, endpoints,
/// and mappers return stable contracts instead of building errors inline.
/// </remarks>
public sealed class InlineErrorUsageTests
{
    // Enforce: Error construction must not appear outside the outcome files below.
    // PermissionMetadata maps FluentValidation codes dynamically; it is a deliberate
    // exception because its codes come from the validator, not from a literal.
    private static readonly string[] AllowedPaths =
    [
        "src/Core/BuildingBlocks/Functional/Results/Result.Generic.cs",
        "src/Application/BuildingBlocks/Security/Authorization/Domain/PermissionMetadata.cs"
    ];

    private static readonly string[] ScannedRoots = ["src", "app"];

    private static readonly Regex ErrorConstructionPattern = new(
        @"\bError\.(?:Custom|BadRequest|Unauthorized|Forbidden|NotFound|Conflict|UnprocessableEntity|Validation|InternalServerError|Unexpected|ServiceUnavailable|FromException)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact(DisplayName = "Error construction should only appear in outcome files")]
    public void ErrorConstruction_ShouldOnlyAppearInOutcomeFiles()
    {
        string root = FindRepositoryRoot();
        List<string> scannedFiles = [];
        List<string> offenders = [];

        foreach (string directory in ScannedRoots)
        {
            string path = Path.Combine(root, directory);
            if (!Directory.Exists(path))
                continue;

            foreach (string file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (IsExcluded(relative))
                    continue;

                scannedFiles.Add(relative);

                string text = File.ReadAllText(file);
                foreach (Match match in ErrorConstructionPattern.Matches(text))
                {
                    int line = 1 + text.AsSpan(0, match.Index).Count('\n');
                    offenders.Add($"{relative}:{line} -> {match.Value}");
                }
            }
        }

        scannedFiles.Should().NotBeEmpty(because: "the scan must find source files to be meaningful");
        offenders.Should().BeEmpty(
            because: "error factories belong in *Outcome.cs files; move the inline construction to the owning outcome");
    }

    [Fact(DisplayName = "The inline-error allowlist should point at existing files")]
    public void Allowlist_ShouldPointAtExistingFiles()
    {
        string root = FindRepositoryRoot();

        string[] missing = AllowedPaths
            .Where(relative => !File.Exists(Path.Combine(root, relative)))
            .ToArray();

        missing.Should().BeEmpty(because: "stale allowlist entries hide real violations");
    }

    private static bool IsExcluded(string relativePath) =>
        relativePath.Contains("/bin/", StringComparison.Ordinal) ||
        relativePath.Contains("/obj/", StringComparison.Ordinal) ||
        relativePath.Contains("/node_modules/", StringComparison.Ordinal) ||
        relativePath.EndsWith("Outcome.cs", StringComparison.Ordinal) ||
        AllowedPaths.Contains(relativePath, StringComparer.Ordinal);

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "QingFa.Shop.slnx")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the repository root (QingFa.Shop.slnx) from the test output directory.");
    }
}
