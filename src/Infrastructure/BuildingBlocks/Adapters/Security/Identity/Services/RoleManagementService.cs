using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Permissions.Services;
using Application.BuildingBlocks.Security.Authorization.Permissions.Store;
using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Roles.Models;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles.Claims;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Roles;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Services;

/// <summary>Admin role management over the ASP.NET Core Identity role store.</summary>
/// <remarks>
/// Every failure is returned as a <see cref="Result{TValue}"/> with a stable error
/// code. Permission mutations go through <see cref="IPermissionService"/> so the
/// role and user authorization caches are invalidated with the claims.
/// </remarks>
public sealed class RoleManagementService(
    RoleManager<Role> roleManager,
    ApplicationDbContext dbContext,
    IPermissionService permissions) : IRoleManagementService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 200;
    private const string SystemActor = "System";

    /// <inheritdoc />
    /// <remarks>
    /// Supported parameters are <c>Search</c>, <c>Page</c>, and <c>PageSize</c>.
    /// The richer query pipeline parameters are rejected with <c>Query.Unsupported</c>
    /// so callers never receive silently ignored input.
    /// </remarks>
    public async Task<Result<QueryPage<RoleModel>>> SearchAsync(
        QueryParameters parameters,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        Result supported = ValidateSearchParameters(parameters);
        if (supported.IsFailure)
            return Result<QueryPage<RoleModel>>.Fail(supported.Errors);

        IQueryable<Role> query = dbContext.Set<Role>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            string search = parameters.Search.Trim().ToLowerInvariant();
            query = query.Where(role =>
                (role.Name != null && role.Name.ToLower().Contains(search)) ||
                (role.Description != null && role.Description.ToLower().Contains(search)) ||
                (role.CreatedBy != null && role.CreatedBy.ToLower().Contains(search)) ||
                (role.ModifiedBy != null && role.ModifiedBy.ToLower().Contains(search)));
        }

        int page = parameters.Page is > 0 ? parameters.Page.Value : 1;
        int pageSize = parameters.PageSize is > 0
            ? Math.Min(parameters.PageSize.Value, MaxPageSize)
            : DefaultPageSize;

        long totalCount = await query.LongCountAsync(ct).ConfigureAwait(false);

        List<Role> roles = await query
            .OrderBy(role => role.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        IReadOnlyDictionary<Guid, IReadOnlyList<string>> rolePermissions =
            await GetPermissionsAsync([.. roles.Select(role => role.Id)], ct)
                .ConfigureAwait(false);

        List<RoleModel> items = [.. roles.Select(role => ToModel(
            role,
            rolePermissions.TryGetValue(role.Id, out IReadOnlyList<string>? values)
                ? values
                : []))];

        return Result<QueryPage<RoleModel>>.Ok(
            new QueryPage<RoleModel>(items, page, pageSize, totalCount));
    }

    /// <inheritdoc />
    public async Task<Result<RoleModel>> GetAsync(Guid id, CancellationToken ct = default)
    {
        Role? role = await roleManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);

        return role is null
            ? Result<RoleModel>.Fail(RoleOutcome.Failure.NotFound)
            : Result<RoleModel>.Ok(await ToModelAsync(role, ct).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<RoleModel>> CreateAsync(
        CreateRoleRequestModel spec,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(spec);

        // Validate: reject malformed permission identifiers before creating the role.
        string? invalidPermission = spec.Permissions.FirstOrDefault(
            permission => !PermissionMetadataConstant.IsValidIdentifier(permission));

        if (invalidPermission is not null)
        {
            return Result<RoleModel>.Fail(
                PermissionStoreOutcome.Failure.InvalidIdentifier(invalidPermission));
        }

        string name = spec.Name.Trim();

        if (await roleManager.FindByNameAsync(name).ConfigureAwait(false) is not null)
            return Result<RoleModel>.Fail(RoleOutcome.Failure.AlreadyExists);

        Result<Role> created = Role.Create(name, spec.Description);
        if (created.IsFailure)
            return Result<RoleModel>.Fail(created.Errors);

        Role role = created.Value;
        role.CreatedAtUtc = DateTimeOffset.UtcNow;
        role.CreatedBy = SystemActor;

        IdentityResult result = await roleManager.CreateAsync(role).ConfigureAwait(false);
        if (!result.Succeeded)
            return Fail<RoleModel>(result);

        if (spec.Permissions.Count > 0)
        {
            Result assigned = await permissions
                .AddRolePermissionsAsync(role.Id, spec.Permissions, ct)
                .ConfigureAwait(false);

            if (assigned.IsFailure)
                return Result<RoleModel>.Fail(assigned.Errors);
        }

        return Result<RoleModel>.Ok(await ToModelAsync(role, ct).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<RoleModel>> UpdateAsync(
        Guid id,
        UpdateRoleRequestModel spec,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(spec);

        Role? role = await roleManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
        if (role is null)
            return Result<RoleModel>.Fail(RoleOutcome.Failure.NotFound);

        Result<Role> updated = Result<Role>
            .Success(role)
            .Update(spec.Name, spec.Description);

        if (updated.IsFailure)
            return Result<RoleModel>.Fail(updated.Errors);

        IdentityResult result = await roleManager.UpdateAsync(role).ConfigureAwait(false);
        if (!result.Succeeded)
            return Fail<RoleModel>(result);

        return Result<RoleModel>.Ok(await ToModelAsync(role, ct).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        Role? role = await roleManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
        if (role is null)
            return Result.Fail(RoleOutcome.Failure.NotFound);

        if (role.IsSystem)
            return Result.Fail(RoleOutcome.Failure.SystemRoleProtected);

        bool assigned = await dbContext.Set<UserRole>()
            .AnyAsync(userRole => userRole.RoleId == role.Id, ct)
            .ConfigureAwait(false);

        if (assigned)
            return Result.Fail(RoleOutcome.Failure.CannotDeleteRoleWithUsers);

        IdentityResult result = await roleManager.DeleteAsync(role).ConfigureAwait(false);
        if (!result.Succeeded)
            return Fail(result);

        // Cache: drop role and user entries that still carry the deleted role's claims.
        return await permissions
            .InvalidateRolePermissionsAsync(role.Id, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<RoleModel>> AssignPermissionAsync(
        Guid roleId,
        string permission,
        CancellationToken ct = default)
    {
        Role? role = await roleManager.FindByIdAsync(roleId.ToString()).ConfigureAwait(false);
        if (role is null)
            return Result<RoleModel>.Fail(RoleOutcome.Failure.NotFound);

        if (!PermissionMetadataConstant.IsValidIdentifier(permission))
        {
            return Result<RoleModel>.Fail(
                PermissionStoreOutcome.Failure.InvalidIdentifier(permission));
        }

        Result assigned = await permissions
            .AddRolePermissionsAsync(role.Id, [permission], ct)
            .ConfigureAwait(false);

        return assigned.IsFailure
            ? Result<RoleModel>.Fail(assigned.Errors)
            : Result<RoleModel>.Ok(await ToModelAsync(role, ct).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<RoleModel>> RemovePermissionAsync(
        Guid roleId,
        string permission,
        CancellationToken ct = default)
    {
        Role? role = await roleManager.FindByIdAsync(roleId.ToString()).ConfigureAwait(false);
        if (role is null)
            return Result<RoleModel>.Fail(RoleOutcome.Failure.NotFound);

        if (!PermissionMetadataConstant.IsValidIdentifier(permission))
        {
            return Result<RoleModel>.Fail(
                PermissionStoreOutcome.Failure.InvalidIdentifier(permission));
        }

        Result removed = await permissions
            .RemoveRolePermissionsAsync(role.Id, [permission], ct)
            .ConfigureAwait(false);

        return removed.IsFailure
            ? Result<RoleModel>.Fail(removed.Errors)
            : Result<RoleModel>.Ok(await ToModelAsync(role, ct).ConfigureAwait(false));
    }

    private static Result ValidateSearchParameters(QueryParameters parameters)
    {
        if (parameters.Filter is not null)
            return UnsupportedSearch(nameof(parameters.Filter));

        if (parameters.OrderBy is not null)
            return UnsupportedSearch(nameof(parameters.OrderBy));

        if (parameters.SearchFields is not null)
            return UnsupportedSearch(nameof(parameters.SearchFields));

        if (parameters.SearchMode is not null)
            return UnsupportedSearch(nameof(parameters.SearchMode));

        if (parameters.SearchTermLogic is not null)
            return UnsupportedSearch(nameof(parameters.SearchTermLogic));

        if (parameters.SearchCaseInsensitive is not null)
            return UnsupportedSearch(nameof(parameters.SearchCaseInsensitive));

        if (parameters.Select is not null)
            return UnsupportedSearch(nameof(parameters.Select));

        if (parameters.GroupBy is not null)
            return UnsupportedSearch(nameof(parameters.GroupBy));

        if (parameters.Aggregate is not null)
            return UnsupportedSearch(nameof(parameters.Aggregate));

        return Result.Ok();
    }

    private static Result UnsupportedSearch(string parameter) => Result.Fail(
        RoleOutcome.Failure.UnsupportedSearch(parameter));

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetPermissionsAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken ct)
    {
        if (roleIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<string>>();

        List<Guid> ids = [.. roleIds];

        List<RolePermissionAssignment> assignments = await (
            from claim in dbContext.Set<RoleClaim>().AsNoTracking()
            where ids.Contains(claim.RoleId) &&
                claim.ClaimType == PermissionMetadataConstant.ClaimType
            select new RolePermissionAssignment(claim.RoleId, claim.ClaimValue))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return assignments
            .GroupBy(assignment => assignment.RoleId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)[
                    .. group
                        .Select(assignment => assignment.Value ?? string.Empty)
                        .Where(value => value.Length > 0)
                        .OrderBy(value => value, StringComparer.Ordinal)]);
    }

    private async Task<RoleModel> ToModelAsync(Role role, CancellationToken ct)
    {
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> rolePermissions =
            await GetPermissionsAsync([role.Id], ct).ConfigureAwait(false);

        return ToModel(
            role,
            rolePermissions.TryGetValue(role.Id, out IReadOnlyList<string>? values)
                ? values
                : []);
    }

    private static RoleModel ToModel(Role role, IReadOnlyList<string> rolePermissions) => new()
    {
        Id = role.Id,
        Name = role.Name ?? string.Empty,
        Description = role.Description,
        Permissions = rolePermissions,
    };

    private static Result<T> Fail<T>(IdentityResult result) =>
        Result<T>.Fail(result.Errors
            .Select(ToError)
            .ToList());

    private static Result Fail(IdentityResult result) =>
        Result.Fail(result.Errors
            .Select(ToError)
            .ToList());

    private static Error ToError(IdentityError error) => error.Code == "DuplicateRoleName"
        ? RoleOutcome.Failure.AlreadyExists
        : RoleOutcome.Failure.OperationFailed(error.Description);

    private sealed record RolePermissionAssignment(Guid RoleId, string? Value);
}
