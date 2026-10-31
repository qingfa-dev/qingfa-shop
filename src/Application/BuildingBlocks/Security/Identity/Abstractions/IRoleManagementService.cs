using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Security.Identity.Roles.Models;

namespace Application.BuildingBlocks.Security.Identity.Abstractions;

/// <summary>Admin-facing port for role and role-permission management.</summary>
/// <remarks>
/// Every operation returns a <see cref="Result{TValue}"/>; identity failures are
/// reported as errors rather than thrown exceptions. Implementations live in
/// Infrastructure and own the ASP.NET Identity role store.
/// </remarks>
public interface IRoleManagementService
{
    /// <summary>Searches roles using the shared query pipeline.</summary>
    /// <param name="parameters">The filter, ordering, search, and paging inputs.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>One page of roles, or a failure.</returns>
    Task<Result<QueryPage<RoleModel>>> SearchAsync(
        QueryParameters parameters,
        CancellationToken ct = default);

    /// <summary>Gets a single role by identifier.</summary>
    /// <param name="id">The role identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The role, or a failure when it does not exist.</returns>
    Task<Result<RoleModel>> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates a role with an initial permission set.</summary>
    /// <param name="spec">The creation values.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created role, or a failure.</returns>
    Task<Result<RoleModel>> CreateAsync(
        CreateRoleRequestModel spec,
        CancellationToken ct = default);

    /// <summary>Updates a role's profile.</summary>
    /// <param name="id">The role identifier.</param>
    /// <param name="spec">The update values.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated role, or a failure.</returns>
    Task<Result<RoleModel>> UpdateAsync(
        Guid id,
        UpdateRoleRequestModel spec,
        CancellationToken ct = default);

    /// <summary>Deletes a role.</summary>
    /// <param name="id">The role identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A success result, or a failure.</returns>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Adds a permission to a role.</summary>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="permission">The permission identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated role, or a failure.</returns>
    Task<Result<RoleModel>> AssignPermissionAsync(
        Guid roleId,
        string permission,
        CancellationToken ct = default);

    /// <summary>Removes a permission from a role.</summary>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="permission">The permission identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated role, or a failure.</returns>
    Task<Result<RoleModel>> RemovePermissionAsync(
        Guid roleId,
        string permission,
        CancellationToken ct = default);
}
