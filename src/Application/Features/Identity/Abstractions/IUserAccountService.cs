using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Features.Identity.Users.Models;

namespace Application.Features.Identity.Abstractions;

/// <summary>Admin-facing port for user account management.</summary>
/// <remarks>
/// Every operation returns a <see cref="Result{TValue}"/>; identity failures are
/// reported as errors rather than thrown exceptions. Implementations live in
/// Infrastructure and own the ASP.NET Identity stores.
/// </remarks>
public interface IUserAccountService
{
    /// <summary>Searches users using the shared query pipeline.</summary>
    /// <param name="parameters">The filter, ordering, search, and paging inputs.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>One page of users, or a failure.</returns>
    Task<Result<QueryPage<UserModel>>> SearchAsync(
        QueryParameters parameters,
        CancellationToken ct = default);

    /// <summary>Gets a single user by identifier.</summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The user, or a failure when it does not exist.</returns>
    Task<Result<UserModel>> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates a local user account.</summary>
    /// <param name="spec">The validated creation values.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created user, or a failure.</returns>
    Task<Result<UserModel>> CreateAsync(CreateUserSpec spec, CancellationToken ct = default);

    /// <summary>Updates the mutable fields of a user.</summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="spec">The partial update values.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated user, or a failure.</returns>
    Task<Result<UserModel>> UpdateAsync(
        Guid id,
        UpdateUserSpec spec,
        CancellationToken ct = default);

    /// <summary>Deletes a user account.</summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A success result, or a failure.</returns>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Activates or deactivates a user account.</summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="active">Whether the account should be active.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated user, or a failure.</returns>
    Task<Result<UserModel>> SetActiveAsync(
        Guid id,
        bool active,
        CancellationToken ct = default);

    /// <summary>Assigns a role to a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated user, or a failure.</returns>
    Task<Result<UserModel>> AssignRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken ct = default);

    /// <summary>Removes a role from a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated user, or a failure.</returns>
    Task<Result<UserModel>> RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken ct = default);

    /// <summary>Resets a user password without requiring the current password.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="newPassword">The replacement plain-text password.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A success result, or a failure.</returns>
    Task<Result> ResetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken ct = default);

    /// <summary>Marks a user email address as confirmed.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A success result, or a failure.</returns>
    Task<Result> ConfirmEmailAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Locks or unlocks a user account.</summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="locked">Whether the account should be locked.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated user, or a failure.</returns>
    Task<Result<UserModel>> SetLockedAsync(
        Guid id,
        bool locked,
        CancellationToken ct = default);

    /// <summary>Revokes every active session for a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A success result, or a failure.</returns>
    Task<Result> RevokeSessionsAsync(Guid userId, CancellationToken ct = default);
}
