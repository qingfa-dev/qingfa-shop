using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

using Application.BuildingBlocks.Security.Authorization;
using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Policies;
using Application.BuildingBlocks.Security.Authorization.Requirements;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Authorization.Policies;

/// <summary>Builds permission policies on demand and delegates everything else to the default provider.</summary>
/// <remarks>
/// A single canonical identifier becomes a permission policy, and encoded
/// <c>permission:all|any:...</c> names become multi-permission policies. New permissions can
/// therefore be granted through role or user claims without recompiling the host. Configured
/// named policies win over permission names, and names that are neither configured nor
/// permission syntax fall back to the host's default provider behavior.
/// </remarks>
public class PermissionPolicyProvider(
    IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    /// <inheritdoc />
    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
            return null;

        // Call: honor an explicitly configured policy before interpreting the name as a permission.
        AuthorizationPolicy? configured = await _fallback.GetPolicyAsync(policyName);
        if (configured is not null)
            return configured;

        if (PermissionPolicyParser.TryParse(policyName, out PermissionPolicyDefinition? definition))
            return Build(definition!.Permissions, definition.MatchMode);

        if (!PermissionMetadataConstant.IsValidIdentifier(policyName))
            return null;

        return Build([PermissionMetadataConstant.NormalizeIdentifier(policyName)], PermissionMatchMode.All);
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();

    private static AuthorizationPolicy Build(
        IReadOnlyList<string> permissions,
        PermissionMatchMode matchMode) =>
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permissions, matchMode))
            .Build();
}
