using System.Text.RegularExpressions;

namespace Application.BuildingBlocks.Security.Authorization.Domain;

/// <summary>
/// Defines the canonical permission syntax, limits, and composition helpers.
/// </summary>
public static class PermissionMetadataConstant
{
    /// <summary>The claim type that carries a permission identifier.</summary>
    public const string ClaimType = "permission";

    /// <summary>Length and character limits for permission metadata.</summary>
    public static class Constraints
    {
        /// <summary>Maximum length of one permission part.</summary>
        public const int MaxPartLength = 50;

        /// <summary>Maximum length of a complete permission identifier.</summary>
        public const int MaxIdentifierLength = 255;

        /// <summary>Maximum length of a permission display name.</summary>
        public const int MaxNameLength = 100;

        /// <summary>Maximum length of a permission description.</summary>
        public const int MaxDescriptionLength = 500;

        /// <summary>Characters accepted inside one permission part.</summary>
        public const string AllowedPartChars = "abcdefghijklmnopqrstuvwxyz0123456789_-";
    }

    /// <summary>Regular expressions that describe the canonical identifier syntax.</summary>
    public static class Patterns
    {
        private static readonly string s_partPattern =
            $"[a-z0-9_-]{{1,{Constraints.MaxPartLength}}}";

        /// <summary>Matches one canonical permission part.</summary>
        public static readonly string Part = $"^{s_partPattern}$";

        /// <summary>Matches a four-part dotted permission identifier.</summary>
        public static readonly string Identifier =
            $"^{s_partPattern}\\.{s_partPattern}\\.{s_partPattern}\\.{s_partPattern}$";
    }

    /// <summary>Default display templates for generated metadata.</summary>
    public static class Defaults
    {
        /// <summary>Template used when no display name is supplied.</summary>
        public const string NameTemplate = "{Action} {Resource}";

        /// <summary>Template used when no description is supplied.</summary>
        public const string DescriptionTemplate = "Allows {Action} on {Resource} in {Domain}/{Category}.";
    }

    // Cache: every part and identifier validation reuses the compiled patterns.
    private static readonly Regex s_partPattern = new(
        Patterns.Part,
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private static readonly Regex s_identifierPattern = new(
        Patterns.Identifier,
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    /// <summary>Gets the cached part matcher.</summary>
    internal static Regex PartPattern => s_partPattern;

    /// <summary>Gets the cached identifier matcher.</summary>
    internal static Regex IdentifierPattern => s_identifierPattern;

    /// <summary>Normalizes one permission part to trimmed lowercase text.</summary>
    /// <param name="value">The candidate part.</param>
    /// <returns>The canonical part, or an empty string when <paramref name="value"/> is null.</returns>
    public static string NormalizePart(string? value) =>
        value?.Trim().ToLowerInvariant() ?? string.Empty;

    /// <summary>Normalizes a permission identifier to trimmed lowercase text.</summary>
    /// <param name="value">The candidate identifier.</param>
    /// <returns>The canonical identifier, or an empty string when <paramref name="value"/> is null.</returns>
    public static string NormalizeIdentifier(string? value) =>
        value?.Trim().ToLowerInvariant() ?? string.Empty;

    /// <summary>Composes canonical permission metadata values.</summary>
    public static class Compose
    {
        /// <summary>Composes the dotted identifier from its four parts.</summary>
        public static string Identifier(
            string domain,
            string category,
            string resource,
            string action) =>
            $"{domain}.{category}.{resource}.{action}".ToLowerInvariant();

        /// <summary>Composes the default display name from the action and resource.</summary>
        public static string DefaultName(string action, string resource) =>
            Defaults.NameTemplate
                .Replace("{Action}", action, StringComparison.Ordinal)
                .Replace("{Resource}", resource, StringComparison.Ordinal);

        /// <summary>Composes the default description from all four parts.</summary>
        public static string DefaultDescription(
            string domain,
            string category,
            string resource,
            string action) =>
            Defaults.DescriptionTemplate
                .Replace("{Action}", action, StringComparison.Ordinal)
                .Replace("{Resource}", resource, StringComparison.Ordinal)
                .Replace("{Domain}", domain, StringComparison.Ordinal)
                .Replace("{Category}", category, StringComparison.Ordinal);
    }

    /// <summary>Checks whether text is a canonical four-part permission identifier.</summary>
    /// <param name="identifier">The candidate identifier; surrounding whitespace and case are ignored.</param>
    /// <returns><c>true</c> when the identifier is canonical; otherwise <c>false</c>.</returns>
    public static bool IsValidIdentifier(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return false;

        return IsCanonicalIdentifier(NormalizeIdentifier(identifier));
    }

    /// <summary>Checks an already-normalized identifier without re-normalizing it.</summary>
    internal static bool IsCanonicalIdentifier(string normalizedIdentifier) =>
        normalizedIdentifier.Length <= Constraints.MaxIdentifierLength &&
        s_identifierPattern.IsMatch(normalizedIdentifier);
}
