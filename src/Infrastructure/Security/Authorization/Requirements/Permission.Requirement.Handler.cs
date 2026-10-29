using Microsoft.Extensions.Logging;

using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;

using Application.Common.Security.Authorization.Permissions.Services;

namespace Application.Common.Security.Authorization.Requirements;

/// <summary>Evaluates permission requirements by resolving effective user permissions through the cache/store pipeline — the admin role bypasses all checks.</summary>
// Invariant: Admin role always succeeds; unauthenticated requests are skipped; failed resolution denies instead of failing open.
// Context: Authorization decisions must never throw — an empty permission set results in denial (Threat TMT-AUTH-001).
// Boundary: Handler → PermissionService — ASP.NET authorization boundary; never accesses the store or cache directly.
public partial class PermissionRequirementAuthorizationHandler(
    IPermissionService permissionService,
    ILogger<PermissionRequirementAuthorizationHandler> logger)
    : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        // Guard: a requirement without identifiers can never be satisfied.
        if (requirement.Permissions.Count == 0)
            return;

        // Guard: skip unauthenticated requests — there is no identity to evaluate.
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        // Call: extract the user identifier from the authenticated principal (nameidentifier or JWT sub).
        string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userId))
        {
            Loggers.LogNoNameIdentifier(logger);
            return;
        }

        // Validate: the identifier must be a Guid before it can be resolved.
        if (!Guid.TryParse(userId, out Guid parsedUserId))
        {
            Loggers.LogInvalidNameIdentifier(logger, userId);
            return;
        }

        string required = Describe(requirement);

        // Guard: the admin role bypasses every permission check.
        if (context.User.IsInRole(AuthorizationConstant.Defaults.AdminRole))
        {
            Loggers.LogAdminBypass(logger, parsedUserId, required);
            context.Succeed(requirement);
            return;
        }

        // Call: resolve effective permissions via the service (module boundary: Handler → PermissionService).
        Result<HashSet<string>> permissionsResult =
            await permissionService.GetEffectiveUserPermissionsAsync(parsedUserId);

        if (permissionsResult.IsFailure)
        {
            // Log: record resolution failure — deny rather than fail open.
            Loggers.LogCacheAccessFailed(
                logger,
                parsedUserId,
                string.Join("; ", permissionsResult.Errors.Select(error => error.Message)));
            return;
        }

        HashSet<string> permissions = permissionsResult.Value;

        // Validate: combine the requirement according to its match mode.
        bool authorized = requirement.MatchMode == PermissionMatchMode.Any
            ? requirement.Permissions.Any(permissions.Contains)
            : requirement.Permissions.All(permissions.Contains);

        if (authorized)
        {
            Loggers.LogAuthorizationSucceeded(logger, parsedUserId, required);
            context.Succeed(requirement);
        }
        else
        {
            Loggers.LogAuthorizationFailed(logger, parsedUserId, required);
        }
    }

    private static string Describe(PermissionRequirement requirement) =>
        requirement.MatchMode == PermissionMatchMode.Any
            ? $"any({string.Join(", ", requirement.Permissions)})"
            : $"all({string.Join(", ", requirement.Permissions)})";
}
