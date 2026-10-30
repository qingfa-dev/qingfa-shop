using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Common.Security.Authentication.Tokens.Abstractions.Refresh;
using Application.Common.Security.Authentication.Tokens.Domain;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Auth.Outcome;
using Application.Features.Identity.Users.Models;
using Application.Features.Identity.Users.Outcome;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Security.Identity.Domain.Roles;
using Infrastructure.Security.Identity.Domain.Users;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security.Identity.Services;

/// <summary>Admin user management over the ASP.NET Core Identity stores.</summary>
/// <remarks>
/// Every failure is returned as a <see cref="Result{TValue}"/> with a stable error
/// code; Identity failures are translated through <see cref="IdentityOutcome"/>.
/// </remarks>
public sealed class UserAccountService(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    ApplicationDbContext dbContext,
    IRefreshTokenService refreshTokens,
    TimeProvider clock) : IUserAccountService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 200;
    private const string SystemActor = "System";

    /// <inheritdoc />
    public async Task<Result<QueryPage<UserModel>>> SearchAsync(
        QueryParameters parameters,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        IQueryable<User> query = dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            string search = parameters.Search.Trim();
            query = query.Where(user =>
                (user.UserName != null && user.UserName.Contains(search)) ||
                (user.Email != null && user.Email.Contains(search)) ||
                user.FirstName.Contains(search) ||
                (user.LastName != null && user.LastName.Contains(search)));
        }

        int page = parameters.Page is > 0 ? parameters.Page.Value : 1;
        int pageSize = parameters.PageSize is > 0
            ? Math.Min(parameters.PageSize.Value, MaxPageSize)
            : DefaultPageSize;

        long totalCount = await query.LongCountAsync(ct).ConfigureAwait(false);

        List<User> users = await query
            .OrderBy(user => user.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        IReadOnlyDictionary<Guid, IReadOnlyList<string>> roles = await GetRolesAsync(
            [.. users.Select(user => user.Id)],
            ct)
            .ConfigureAwait(false);

        List<UserModel> items = [.. users.Select(user => ToModel(
            user,
            roles.TryGetValue(user.Id, out IReadOnlyList<string>? names) ? names : []))];

        return Result<QueryPage<UserModel>>.Ok(
            new QueryPage<UserModel>(items, page, pageSize, totalCount));
    }

    /// <inheritdoc />
    public async Task<Result<UserModel>> GetAsync(Guid id, CancellationToken ct = default)
    {
        User? user = await userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);

        return user is null
            ? Result<UserModel>.Fail(UserAccountOutcome.Failure.UserNotFound)
            : Result<UserModel>.Ok(await ToModelAsync(user).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<UserModel>> CreateAsync(
        CreateUserSpec spec,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(spec);

        if (await userManager.FindByNameAsync(spec.UserName).ConfigureAwait(false) is not null)
            return Result<UserModel>.Fail(IdentityOutcome.Failure.UserNameTaken);

        if (await userManager.FindByEmailAsync(spec.Email).ConfigureAwait(false) is not null)
            return Result<UserModel>.Fail(IdentityOutcome.Failure.EmailTaken);

        Result<IReadOnlyList<Role>> roles = await FindRolesAsync(spec.RoleIds).ConfigureAwait(false);
        if (roles.IsFailure)
            return Result<UserModel>.Fail(roles.Errors);

        Result<User> created = User.Create(
            spec.UserName,
            spec.Email,
            spec.FirstName ?? spec.UserName,
            spec.LastName,
            emailConfirmed: spec.EmailConfirmed);

        if (created.IsFailure)
            return Result<UserModel>.Fail(created.Errors);

        User user = created.Value;
        user.IsActive = true;
        user.CreatedAtUtc = clock.GetUtcNow();
        user.CreatedBy = SystemActor;

        IdentityResult result = await userManager
            .CreateAsync(user, spec.Password)
            .ConfigureAwait(false);

        if (!result.Succeeded)
            return Fail<UserModel>(result);

        foreach (Role role in roles.Value)
        {
            IdentityResult added = await userManager
                .AddToRoleAsync(user, role.Name!)
                .ConfigureAwait(false);

            if (!added.Succeeded)
                return Fail<UserModel>(added);
        }

        return Result<UserModel>.Ok(await ToModelAsync(user).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<UserModel>> UpdateAsync(
        Guid id,
        UpdateUserSpec spec,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(spec);

        User? user = await userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
        if (user is null)
            return Result<UserModel>.Fail(UserAccountOutcome.Failure.UserNotFound);

        if (!string.IsNullOrWhiteSpace(spec.Email) &&
            !string.Equals(spec.Email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            IdentityResult email = await userManager
                .SetEmailAsync(user, spec.Email)
                .ConfigureAwait(false);

            if (!email.Succeeded)
                return Fail<UserModel>(email);

            user.EmailConfirmed = false;
        }

        if (spec.FirstName is not null)
            user.FirstName = spec.FirstName;

        if (spec.LastName is not null)
            user.LastName = spec.LastName;

        if (spec.RoleIds is not null)
        {
            Result roles = await ReplaceRolesAsync(user, spec.RoleIds).ConfigureAwait(false);
            if (roles.IsFailure)
                return Result<UserModel>.Fail(roles.Errors);
        }

        user.ModifiedAtUtc = clock.GetUtcNow();
        user.ModifiedBy = SystemActor;

        IdentityResult update = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!update.Succeeded)
            return Fail<UserModel>(update);

        return Result<UserModel>.Ok(await ToModelAsync(user).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        User? user = await userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
        if (user is null)
            return Result.Fail(UserAccountOutcome.Failure.UserNotFound);

        IdentityResult result = await userManager.DeleteAsync(user).ConfigureAwait(false);
        return result.Succeeded ? Result.Ok() : Fail(result);
    }

    /// <inheritdoc />
    public async Task<Result<UserModel>> SetActiveAsync(
        Guid id,
        bool active,
        CancellationToken ct = default)
    {
        User? user = await userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
        if (user is null)
            return Result<UserModel>.Fail(UserAccountOutcome.Failure.UserNotFound);

        if (user.IsActive != active)
        {
            user.IsActive = active;
            user.ModifiedAtUtc = clock.GetUtcNow();
            user.ModifiedBy = SystemActor;

            IdentityResult update = await userManager.UpdateAsync(user).ConfigureAwait(false);
            if (!update.Succeeded)
                return Fail<UserModel>(update);
        }

        return Result<UserModel>.Ok(await ToModelAsync(user).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<UserModel>> AssignRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken ct = default)
    {
        User? user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return Result<UserModel>.Fail(UserAccountOutcome.Failure.UserNotFound);

        Role? role = await roleManager.FindByIdAsync(roleId.ToString()).ConfigureAwait(false);
        if (role is null || string.IsNullOrWhiteSpace(role.Name))
            return Result<UserModel>.Fail(UserAccountOutcome.Failure.RoleNotFound);

        if (!await userManager.IsInRoleAsync(user, role.Name).ConfigureAwait(false))
        {
            IdentityResult added = await userManager
                .AddToRoleAsync(user, role.Name)
                .ConfigureAwait(false);

            if (!added.Succeeded)
                return Fail<UserModel>(added);
        }

        return Result<UserModel>.Ok(await ToModelAsync(user).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<UserModel>> RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken ct = default)
    {
        User? user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return Result<UserModel>.Fail(UserAccountOutcome.Failure.UserNotFound);

        Role? role = await roleManager.FindByIdAsync(roleId.ToString()).ConfigureAwait(false);
        if (role is null || string.IsNullOrWhiteSpace(role.Name))
            return Result<UserModel>.Fail(UserAccountOutcome.Failure.RoleNotFound);

        if (await userManager.IsInRoleAsync(user, role.Name).ConfigureAwait(false))
        {
            IdentityResult removed = await userManager
                .RemoveFromRoleAsync(user, role.Name)
                .ConfigureAwait(false);

            if (!removed.Succeeded)
                return Fail<UserModel>(removed);
        }

        return Result<UserModel>.Ok(await ToModelAsync(user).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken ct = default)
    {
        User? user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return Result.Fail(UserAccountOutcome.Failure.UserNotFound);

        string token = await userManager
            .GeneratePasswordResetTokenAsync(user)
            .ConfigureAwait(false);

        IdentityResult result = await userManager
            .ResetPasswordAsync(user, token, newPassword)
            .ConfigureAwait(false);

        return result.Succeeded ? Result.Ok() : Fail(result);
    }

    /// <inheritdoc />
    public async Task<Result> ConfirmEmailAsync(Guid userId, CancellationToken ct = default)
    {
        User? user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return Result.Fail(UserAccountOutcome.Failure.UserNotFound);

        if (user.EmailConfirmed)
            return Result.Ok();

        string token = await userManager
            .GenerateEmailConfirmationTokenAsync(user)
            .ConfigureAwait(false);

        IdentityResult result = await userManager
            .ConfirmEmailAsync(user, token)
            .ConfigureAwait(false);

        return result.Succeeded ? Result.Ok() : Fail(result);
    }

    /// <inheritdoc />
    public async Task<Result<UserModel>> SetLockedAsync(
        Guid id,
        bool locked,
        CancellationToken ct = default)
    {
        User? user = await userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
        if (user is null)
            return Result<UserModel>.Fail(UserAccountOutcome.Failure.UserNotFound);

        if (locked && !user.LockoutEnabled)
        {
            IdentityResult enabled = await userManager
                .SetLockoutEnabledAsync(user, true)
                .ConfigureAwait(false);

            if (!enabled.Succeeded)
                return Fail<UserModel>(enabled);
        }

        IdentityResult result = await userManager
            .SetLockoutEndDateAsync(user, locked ? DateTimeOffset.MaxValue : null)
            .ConfigureAwait(false);

        if (!result.Succeeded)
            return Fail<UserModel>(result);

        return Result<UserModel>.Ok(await ToModelAsync(user).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result> RevokeSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        User? user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return Result.Fail(UserAccountOutcome.Failure.UserNotFound);

        IdentityResult stamp = await userManager
            .UpdateSecurityStampAsync(user)
            .ConfigureAwait(false);

        if (!stamp.Succeeded)
            return Fail(stamp);

        Result<int> revoked = await refreshTokens
            .RevokeAllAsync(
                user.Id.ToString(),
                clock.GetUtcNow(),
                RefreshTokenRevocationReason.UserLogoutAll,
                ct)
            .ConfigureAwait(false);

        return revoked.ToResult();
    }

    private async Task<Result> ReplaceRolesAsync(User user, IReadOnlyList<Guid> roleIds)
    {
        Result<IReadOnlyList<Role>> roles = await FindRolesAsync(roleIds).ConfigureAwait(false);
        if (roles.IsFailure)
            return Result.Fail(roles.Errors);

        HashSet<string> desired = new(
            roles.Value
                .Select(role => role.Name)
                .OfType<string>()
                .Where(name => !string.IsNullOrWhiteSpace(name)),
            StringComparer.OrdinalIgnoreCase);

        IList<string> current = await userManager.GetRolesAsync(user).ConfigureAwait(false);

        foreach (string role in current.Where(role => !desired.Contains(role)))
        {
            IdentityResult removed = await userManager
                .RemoveFromRoleAsync(user, role)
                .ConfigureAwait(false);

            if (!removed.Succeeded)
                return Fail(removed);
        }

        foreach (string role in desired.Where(role =>
            !current.Contains(role, StringComparer.OrdinalIgnoreCase)))
        {
            IdentityResult added = await userManager
                .AddToRoleAsync(user, role)
                .ConfigureAwait(false);

            if (!added.Succeeded)
                return Fail(added);
        }

        return Result.Ok();
    }

    private async Task<Result<IReadOnlyList<Role>>> FindRolesAsync(
        IReadOnlyList<Guid> roleIds)
    {
        if (roleIds.Count == 0)
            return Result<IReadOnlyList<Role>>.Ok([]);

        List<Role> roles = [];

        foreach (Guid roleId in roleIds.Distinct())
        {
            Role? role = await roleManager.FindByIdAsync(roleId.ToString()).ConfigureAwait(false);
            if (role is null)
                return Result<IReadOnlyList<Role>>.Fail(UserAccountOutcome.Failure.RoleNotFound);

            roles.Add(role);
        }

        return Result<IReadOnlyList<Role>>.Ok(roles);
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetRolesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken ct)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<string>>();

        List<Guid> ids = [.. userIds];

        List<RoleAssignment> assignments = await (
            from userRole in dbContext.UserRoles
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where ids.Contains(userRole.UserId)
            select new RoleAssignment(userRole.UserId, role.Name))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return assignments
            .GroupBy(assignment => assignment.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)[
                    .. group
                        .Select(assignment => assignment.Name ?? string.Empty)
                        .Where(name => name.Length > 0)
                        .OrderBy(name => name, StringComparer.Ordinal)]);
    }

    private async Task<UserModel> ToModelAsync(User user)
    {
        IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        return ToModel(user, [.. roles.OrderBy(role => role, StringComparer.Ordinal)]);
    }

    private UserModel ToModel(User user, IReadOnlyList<string> roles)
    {
        DateTimeOffset nowUtc = clock.GetUtcNow();
        bool isLockedOut = user.LockoutEnd is not null && user.LockoutEnd > nowUtc;

        return new UserModel(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            user.IsActive,
            isLockedOut,
            user.LockoutEnd,
            roles);
    }

    private static Result<T> Fail<T>(IdentityResult result) =>
        Result<T>.Fail(result.Errors
            .Select(error => IdentityOutcome.Failure.IdentityError(error.Code, error.Description))
            .ToList());

    private static Result Fail(IdentityResult result) =>
        Result.Fail(result.Errors
            .Select(error => IdentityOutcome.Failure.IdentityError(error.Code, error.Description))
            .ToList());

    private sealed record RoleAssignment(Guid UserId, string? Name);
}
