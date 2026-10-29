using Application.Common.Security.Authorization.Domain;
using Application.Common.Security.Authorization.Permissions.Store;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Security.Identity.Domain.Roles.Claims;
using Infrastructure.Security.Identity.Domain.Users.Claims;
using Infrastructure.Security.Identity.Domain.Users.Roles;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security.Authorization.Permissions.Store;

/// <summary>Reads and writes permission claims through the Identity claim tables.</summary>
// Invariant: only claims with ClaimType=permission are permissions; every read returns a case-insensitive distinct set.
// Context: the store reports specific failures; authorization callers degrade to an empty set so a storage outage denies access instead of failing open (Threat TMT-AUTH-001).
// Boundary: Store → Persistence — pure data access; no cache orchestration or policy decisions.
public sealed partial class PermissionStoreService(
    ApplicationDbContext dbContext,
    ILogger<PermissionStoreService> logger) : IPermissionStore
{
    private readonly ILogger<PermissionStoreService> _logger = logger;

    /// <summary>Returns all permissions for a user by merging role-based claims and direct user claims.</summary>
    // Contract: pre=userId!=Guid.Empty, post=return.IsSuccess && return.Value!=null, throws=never
    public async Task<Result<HashSet<string>>> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        try
        {
            // Call: fetch permission claims from every role assigned to the user.
            List<string> roleClaims = await dbContext.Set<UserRole>()
                .AsNoTracking()
                .Where(userRole => userRole.UserId == userId)
                .Join(
                    dbContext.Set<RoleClaim>().AsNoTracking(),
                    userRole => userRole.RoleId,
                    roleClaim => roleClaim.RoleId,
                    (_, roleClaim) => roleClaim)
                .Where(roleClaim => roleClaim.ClaimType == PermissionMetadataConstant.ClaimType)
                .Select(roleClaim => roleClaim.ClaimValue!)
                .Distinct()
                .ToListAsync(ct);

            // Call: fetch permission claims assigned directly to the user account.
            List<string> userClaims = await dbContext.Set<UserClaim>()
                .AsNoTracking()
                .Where(userClaim =>
                    userClaim.UserId == userId &&
                    userClaim.ClaimType == PermissionMetadataConstant.ClaimType)
                .Select(userClaim => userClaim.ClaimValue!)
                .Distinct()
                .ToListAsync(ct);

            // Transform: merge both sources into a single case-insensitive set.
            var permissions = new HashSet<string>(roleClaims, StringComparer.OrdinalIgnoreCase);
            permissions.UnionWith(userClaims);

            Loggers.LogPermissionsLoaded(_logger, permissions.Count, userId);
            return Result<HashSet<string>>.Ok(permissions);
        }
        catch (Exception ex)
        {
            // Catch: report a specific read failure; authorization callers degrade to an empty set.
            Loggers.LogGetPermissionsFailed(_logger, userId, ex);
            return Result<HashSet<string>>.Fail(
                PermissionStoreOutcome.Failure.UserPermissionsReadFailed(userId, ex));
        }
    }

    /// <summary>Returns all permissions assigned directly to a role.</summary>
    // Contract: pre=roleId!=Guid.Empty, post=return.IsSuccess && return.Value!=null, throws=never
    public async Task<Result<HashSet<string>>> GetRolePermissionsAsync(
        Guid roleId,
        CancellationToken ct = default)
    {
        try
        {
            // Call: fetch distinct permission claims for the role.
            List<string> roleClaims = await dbContext.Set<RoleClaim>()
                .AsNoTracking()
                .Where(roleClaim =>
                    roleClaim.RoleId == roleId &&
                    roleClaim.ClaimType == PermissionMetadataConstant.ClaimType)
                .Select(roleClaim => roleClaim.ClaimValue!)
                .Distinct()
                .ToListAsync(ct);

            var permissions = new HashSet<string>(roleClaims, StringComparer.OrdinalIgnoreCase);

            Loggers.LogRolePermissionsLoaded(_logger, permissions.Count, roleId);
            return Result<HashSet<string>>.Ok(permissions);
        }
        catch (Exception ex)
        {
            // Catch: report a specific read failure for the role.
            Loggers.LogGetRolePermissionsFailed(_logger, roleId, ex);
            return Result<HashSet<string>>.Fail(
                PermissionStoreOutcome.Failure.RolePermissionsReadFailed(roleId, ex));
        }
    }

    /// <summary>Returns the set of role IDs assigned to a user.</summary>
    // Contract: pre=userId!=Guid.Empty, post=return.IsSuccess, throws=never
    public async Task<Result<HashSet<Guid>>> GetUserRoleIdsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        try
        {
            List<Guid> roleIds = await dbContext.Set<UserRole>()
                .AsNoTracking()
                .Where(userRole => userRole.UserId == userId)
                .Select(userRole => userRole.RoleId)
                .Distinct()
                .ToListAsync(ct);

            return Result<HashSet<Guid>>.Ok([.. roleIds]);
        }
        catch (Exception ex)
        {
            // Catch: report the role-id read failure; the caller resolves direct claims only.
            Loggers.LogGetPermissionsFailed(_logger, userId, ex);
            return Result<HashSet<Guid>>.Fail(
                PermissionStoreOutcome.Failure.UserRoleIdsReadFailed(userId, ex));
        }
    }

    /// <summary>Returns only the direct (non-role) permissions assigned to a user.</summary>
    // Contract: pre=userId!=Guid.Empty, post=return.IsSuccess && return.Value!=null, throws=never
    public async Task<Result<HashSet<string>>> GetUserDirectPermissionsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        try
        {
            List<string> userClaims = await dbContext.Set<UserClaim>()
                .AsNoTracking()
                .Where(userClaim =>
                    userClaim.UserId == userId &&
                    userClaim.ClaimType == PermissionMetadataConstant.ClaimType)
                .Select(userClaim => userClaim.ClaimValue!)
                .Distinct()
                .ToListAsync(ct);

            var permissions = new HashSet<string>(userClaims, StringComparer.OrdinalIgnoreCase);

            Loggers.LogPermissionsLoaded(_logger, permissions.Count, userId);
            return Result<HashSet<string>>.Ok(permissions);
        }
        catch (Exception ex)
        {
            // Catch: report a specific direct-permission read failure.
            Loggers.LogGetPermissionsFailed(_logger, userId, ex);
            return Result<HashSet<string>>.Fail(
                PermissionStoreOutcome.Failure.UserDirectPermissionsReadFailed(userId, ex));
        }
    }

    /// <summary>Adds permissions to a role, skipping identifiers the role already holds.</summary>
    // Contract: pre=roleId!=Guid.Empty && permissions!=null, post=return.IsSuccess if committed, throws=never
    public async Task<Result> AddRolePermissionsAsync(
        Guid roleId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        Result<List<string>> prepared = PrepareForWrite(permissions);
        if (prepared.IsFailure)
            return Result.Fail(prepared.Errors);

        List<string> requested = prepared.Value;
        if (requested.Count == 0)
            return Result.Ok();

        try
        {
            // Call: query existing claims so re-granting a permission is idempotent.
            List<string> existing = await dbContext.Set<RoleClaim>()
                .AsNoTracking()
                .Where(roleClaim =>
                    roleClaim.RoleId == roleId &&
                    roleClaim.ClaimType == PermissionMetadataConstant.ClaimType)
                .Select(roleClaim => roleClaim.ClaimValue!)
                .ToListAsync(ct);

            var known = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
            List<string> missing = [.. requested.Where(permission => !known.Contains(permission))];

            if (missing.Count == 0)
                return Result.Ok();

            dbContext.Set<RoleClaim>().AddRange(missing.Select(permission => new RoleClaim
            {
                RoleId = roleId,
                ClaimType = PermissionMetadataConstant.ClaimType,
                ClaimValue = permission,
            }));

            await dbContext.SaveChangesAsync(ct);

            Loggers.LogBatchAddRolePermissions(_logger, missing.Count, roleId);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            Loggers.LogBatchAddRolePermissionsFailed(_logger, roleId, ex.Message);
            return Result.Fail(
                PermissionStoreOutcome.Failure.RolePermissionsAddFailed(roleId, ex));
        }
    }

    /// <summary>Removes permissions from a role.</summary>
    // Contract: pre=roleId!=Guid.Empty && permissions!=null, post=return.IsSuccess if committed, throws=never
    public async Task<Result> RemoveRolePermissionsAsync(
        Guid roleId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        Result<List<string>> prepared = PrepareForWrite(permissions);
        if (prepared.IsFailure)
            return Result.Fail(prepared.Errors);

        List<string> requested = prepared.Value;
        if (requested.Count == 0)
            return Result.Ok();

        try
        {
            var requestedSet = new HashSet<string>(requested, StringComparer.OrdinalIgnoreCase);

            List<RoleClaim> claimsToRemove = await dbContext.Set<RoleClaim>()
                .Where(roleClaim =>
                    roleClaim.RoleId == roleId &&
                    roleClaim.ClaimType == PermissionMetadataConstant.ClaimType &&
                    roleClaim.ClaimValue != null &&
                    requestedSet.Contains(roleClaim.ClaimValue))
                .ToListAsync(ct);

            if (claimsToRemove.Count == 0)
                return Result.Ok();

            dbContext.Set<RoleClaim>().RemoveRange(claimsToRemove);
            await dbContext.SaveChangesAsync(ct);

            Loggers.LogBatchRemoveRolePermissions(_logger, claimsToRemove.Count, roleId);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            Loggers.LogBatchRemoveRolePermissionsFailed(_logger, roleId, ex.Message);
            return Result.Fail(
                PermissionStoreOutcome.Failure.RolePermissionsRemoveFailed(roleId, ex));
        }
    }

    /// <summary>Adds direct permissions to a user, skipping identifiers the user already holds directly.</summary>
    // Contract: pre=userId!=Guid.Empty && permissions!=null, post=return.IsSuccess if committed, throws=never
    public async Task<Result> AddUserDirectPermissionsAsync(
        Guid userId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        Result<List<string>> prepared = PrepareForWrite(permissions);
        if (prepared.IsFailure)
            return Result.Fail(prepared.Errors);

        List<string> requested = prepared.Value;
        if (requested.Count == 0)
            return Result.Ok();

        try
        {
            // Call: query existing direct claims so re-granting a permission is idempotent.
            List<string> existing = await dbContext.Set<UserClaim>()
                .AsNoTracking()
                .Where(userClaim =>
                    userClaim.UserId == userId &&
                    userClaim.ClaimType == PermissionMetadataConstant.ClaimType)
                .Select(userClaim => userClaim.ClaimValue!)
                .ToListAsync(ct);

            var known = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
            List<string> missing = [.. requested.Where(permission => !known.Contains(permission))];

            if (missing.Count == 0)
                return Result.Ok();

            dbContext.Set<UserClaim>().AddRange(missing.Select(permission => new UserClaim
            {
                UserId = userId,
                ClaimType = PermissionMetadataConstant.ClaimType,
                ClaimValue = permission,
            }));

            await dbContext.SaveChangesAsync(ct);

            Loggers.LogBatchAddUserPermissions(_logger, missing.Count, userId);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            Loggers.LogBatchAddUserPermissionsFailed(_logger, userId, ex.Message);
            return Result.Fail(
                PermissionStoreOutcome.Failure.UserPermissionsAddFailed(userId, ex));
        }
    }

    /// <summary>Removes direct permissions from a user.</summary>
    // Contract: pre=userId!=Guid.Empty && permissions!=null, post=return.IsSuccess if committed, throws=never
    public async Task<Result> RemoveUserDirectPermissionsAsync(
        Guid userId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        Result<List<string>> prepared = PrepareForWrite(permissions);
        if (prepared.IsFailure)
            return Result.Fail(prepared.Errors);

        List<string> requested = prepared.Value;
        if (requested.Count == 0)
            return Result.Ok();

        try
        {
            var requestedSet = new HashSet<string>(requested, StringComparer.OrdinalIgnoreCase);

            List<UserClaim> claimsToRemove = await dbContext.Set<UserClaim>()
                .Where(userClaim =>
                    userClaim.UserId == userId &&
                    userClaim.ClaimType == PermissionMetadataConstant.ClaimType &&
                    userClaim.ClaimValue != null &&
                    requestedSet.Contains(userClaim.ClaimValue))
                .ToListAsync(ct);

            if (claimsToRemove.Count == 0)
                return Result.Ok();

            dbContext.Set<UserClaim>().RemoveRange(claimsToRemove);
            await dbContext.SaveChangesAsync(ct);

            Loggers.LogBatchRemoveUserPermissions(_logger, claimsToRemove.Count, userId);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            Loggers.LogBatchRemoveUserPermissionsFailed(_logger, userId, ex.Message);
            return Result.Fail(
                PermissionStoreOutcome.Failure.UserPermissionsRemoveFailed(userId, ex));
        }
    }

    /// <summary>Loads every distinct permission identifier across role and user claims.</summary>
    // Contract: post=return.IsSuccess && return.Value!=null, throws=never
    public async Task<Result<HashSet<string>>> GetAllPermissionIdentifiersAsync(
        CancellationToken ct = default)
    {
        try
        {
            List<string> roleIdentifiers = await dbContext.Set<RoleClaim>()
                .AsNoTracking()
                .Where(roleClaim => roleClaim.ClaimType == PermissionMetadataConstant.ClaimType)
                .Select(roleClaim => roleClaim.ClaimValue!)
                .Distinct()
                .ToListAsync(ct);

            List<string> userIdentifiers = await dbContext.Set<UserClaim>()
                .AsNoTracking()
                .Where(userClaim => userClaim.ClaimType == PermissionMetadataConstant.ClaimType)
                .Select(userClaim => userClaim.ClaimValue!)
                .Distinct()
                .ToListAsync(ct);

            var identifiers = new HashSet<string>(roleIdentifiers, StringComparer.OrdinalIgnoreCase);
            identifiers.UnionWith(userIdentifiers);

            Loggers.LogAllIdentifiersLoaded(_logger, identifiers.Count);
            return Result<HashSet<string>>.Ok(identifiers);
        }
        catch (Exception ex)
        {
            // Catch: report the catalog read failure.
            Loggers.LogGetPermissionsFailed(_logger, Guid.Empty, ex);
            return Result<HashSet<string>>.Fail(
                PermissionStoreOutcome.Failure.AllIdentifiersReadFailed(ex));
        }
    }

    // Validate: canonicalize, de-duplicate, and reject malformed identifiers before any write.
    private static Result<List<string>> PrepareForWrite(IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        List<string> normalized = [.. permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(PermissionMetadataConstant.NormalizeIdentifier)
            .Distinct(StringComparer.Ordinal)];

        if (normalized.Count > PermissionStoreConstant.Constraints.MaxPermissionsPerBatch)
        {
            return Result<List<string>>.Fail(
                PermissionStoreOutcome.Failure.BatchTooLarge(normalized.Count));
        }

        string? invalid = normalized.FirstOrDefault(
            permission => !PermissionMetadataConstant.IsValidIdentifier(permission));

        return invalid is null
            ? Result<List<string>>.Ok(normalized)
            : Result<List<string>>.Fail(
                PermissionStoreOutcome.Failure.InvalidIdentifier(invalid));
    }
}
