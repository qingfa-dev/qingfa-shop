using Application.BuildingBlocks.Security.Authorization.Domain;

namespace Application.BuildingBlocks.Security.Authorization.Policies;

/// <summary>Composes and parses policy names that carry more than one permission.</summary>
/// <remarks>
/// A single permission keeps its canonical identifier as the policy name, so existing
/// <c>[Authorize(Policy = "domain.category.resource.action")]</c> declarations keep working.
/// Multiple permissions use <c>permission:&lt;all|any&gt;:&lt;id&gt;|&lt;id&gt;</c>; canonical
/// identifiers cannot contain <c>:</c> or <c>|</c>, so the encoding is unambiguous.
/// </remarks>
public static class PermissionPolicyParser
{
    private const string Prefix = "permission";
    private const char ModeSeparator = ':';
    private const char PermissionSeparator = '|';

    /// <summary>Composes a policy name for the supplied permissions.</summary>
    /// <param name="permissions">The permission identifiers.</param>
    /// <param name="matchMode">How the identifiers combine.</param>
    /// <returns>The canonical single identifier or the encoded multi-permission name.</returns>
    /// <exception cref="ArgumentException">No permission identifier was supplied.</exception>
    public static string Compose(
        IEnumerable<string> permissions,
        PermissionMatchMode matchMode)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        Result<string> result = TryCompose(permissions, matchMode);

        // Boundary: the attribute constructor cannot return Result; callers inside a flow
        // must use TryCompose instead.
        return result.IsSuccess
            ? result.Value
            : throw new ArgumentException(result.Errors[0].Message, nameof(permissions));
    }

    /// <summary>Composes a policy name without throwing.</summary>
    /// <param name="permissions">The permission identifiers.</param>
    /// <param name="matchMode">How the identifiers combine.</param>
    /// <returns>The composed policy name, or a stable validation failure.</returns>
    public static Result<string> TryCompose(
        IEnumerable<string> permissions,
        PermissionMatchMode matchMode)
    {
        if (permissions is null)
            return Result<string>.Fail(PermissionPolicyOutcome.Failure.NoPermissions);

        string[] normalized = [.. permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(PermissionMetadataConstant.NormalizeIdentifier)
            .Distinct(StringComparer.Ordinal)];

        if (normalized.Length == 0)
            return Result<string>.Fail(PermissionPolicyOutcome.Failure.NoPermissions);

        string? invalid = normalized.FirstOrDefault(
            permission => !PermissionMetadataConstant.IsValidIdentifier(permission));

        if (invalid is not null)
            return Result<string>.Fail(PermissionPolicyOutcome.Failure.InvalidIdentifier(invalid));

        if (normalized.Length == 1)
            return Result<string>.Success(normalized[0]);

        string mode = matchMode == PermissionMatchMode.Any ? "any" : "all";
        return Result<string>.Success(
            $"{Prefix}{ModeSeparator}{mode}{ModeSeparator}{string.Join(PermissionSeparator, normalized)}");
    }

    /// <summary>Attempts to parse an encoded multi-permission policy name.</summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="definition">The parsed definition when the name is encoded permission syntax.</param>
    /// <returns><c>true</c> when the name is an encoded permission policy; otherwise <c>false</c>.</returns>
    public static bool TryParse(
        string? policyName,
        out PermissionPolicyDefinition? definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(policyName) ||
            !policyName.StartsWith($"{Prefix}{ModeSeparator}", StringComparison.Ordinal))
        {
            return false;
        }

        string[] parts = policyName.Split(ModeSeparator, count: 3);
        if (parts.Length != 3)
            return false;

        PermissionMatchMode matchMode = parts[1] switch
        {
            "all" => PermissionMatchMode.All,
            "any" => PermissionMatchMode.Any,
            _ => (PermissionMatchMode)(-1),
        };

        if (!Enum.IsDefined(matchMode))
            return false;

        string[] permissions = [.. parts[2]
            .Split(PermissionSeparator)
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(PermissionMetadataConstant.NormalizeIdentifier)
            .Distinct(StringComparer.Ordinal)];

        if (permissions.Length == 0 ||
            permissions.Any(permission => !PermissionMetadataConstant.IsValidIdentifier(permission)))
        {
            return false;
        }

        definition = new PermissionPolicyDefinition(permissions, matchMode);
        return true;
    }
}

/// <summary>Parsed permission policy: identifiers plus the match mode.</summary>
/// <param name="Permissions">The permission identifiers.</param>
/// <param name="MatchMode">How the identifiers combine.</param>
public sealed record PermissionPolicyDefinition(
    IReadOnlyList<string> Permissions,
    PermissionMatchMode MatchMode);
