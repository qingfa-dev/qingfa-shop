using Application.BuildingBlocks.Security.Identity.Auth;
using Application.BuildingBlocks.Security.Identity.Auth.Models;
using Application.BuildingBlocks.Security.Identity.Auth.Outcome;
using Application.BuildingBlocks.Security.Identity.Auth.Validators;
using Application.BuildingBlocks.Security.Identity.External.Abstractions;
using Application.BuildingBlocks.Security.Identity.External.Models;
using Application.BuildingBlocks.Security.Identity.External.Options;

using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users;

using Microsoft.AspNetCore.Identity;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Services;

/// <summary>Creates/links local accounts for external OAuth and OIDC identities.</summary>
public sealed class ExternalAuthenticationService(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IdentityTokenIssuer tokenIssuer,
    ExternalAuthOption option) : IExternalAuthenticationService
{
    /// <inheritdoc />
    public IReadOnlyCollection<ExternalProviderModel> GetProviders()
    {
        List<ExternalProviderModel> providers = [];

        if (option.Google.Enabled)
            providers.Add(new ExternalProviderModel(IdentityConstant.Providers.Google, "Google"));

        if (option.Microsoft.Enabled)
            providers.Add(new ExternalProviderModel(IdentityConstant.Providers.Microsoft, "Microsoft"));

        if (option.OpenIdConnect.Enabled)
        {
            providers.Add(new ExternalProviderModel(
                IdentityConstant.Providers.OpenIdConnect,
                "OpenID Connect"));
        }

        return providers;
    }

    /// <inheritdoc />
    public async Task<Result<AuthTokenResponse>> LoginAsync(
        ExternalUserInfo user,
        CancellationToken cancellationToken = default)
    {
        Result<ExternalUserInfo> validation = IdentityValidator.Validate(user);
        if (validation.IsFailure)
            return Result<AuthTokenResponse>.Fail(validation.Errors);

        if (!GetProviders().Any(provider =>
            string.Equals(provider.Name, user.Provider, StringComparison.OrdinalIgnoreCase)))
        {
            return Result<AuthTokenResponse>.Fail(
                IdentityOutcome.Failure.ExternalProviderUnsupported(user.Provider));
        }

        User? existing = await userManager
            .FindByLoginAsync(user.Provider, user.ProviderKey)
            .ConfigureAwait(false);

        if (existing is null && !string.IsNullOrWhiteSpace(user.Email))
        {
            existing = await userManager.FindByEmailAsync(user.Email).ConfigureAwait(false);
        }

        if (existing is not null)
        {
            if (!existing.IsActive)
                return Result<AuthTokenResponse>.Fail(IdentityOutcome.Failure.Inactive);

            if (await userManager
                .FindByLoginAsync(user.Provider, user.ProviderKey)
                .ConfigureAwait(false) is null)
            {
                IdentityResult linked = await userManager
                    .AddLoginAsync(existing, new UserLoginInfo(user.Provider, user.ProviderKey, user.Provider))
                    .ConfigureAwait(false);

                if (!linked.Succeeded)
                    return Fail<AuthTokenResponse>(linked);
            }

            Result<AuthResponse> issued = await tokenIssuer
                .IssueAsync(existing, cancellationToken)
                .ConfigureAwait(false);

            return issued.Map(response => response.ToTokenResponse());
        }

        string userName = BuildUserName(user);
        string email = string.IsNullOrWhiteSpace(user.Email)
            ? $"{user.ProviderKey}@external.local"
            : user.Email;

        Result<User> created = User.Create(
            userName,
            email,
            user.DisplayName ?? userName,
            lastName: null);

        if (created.IsFailure)
            return Result<AuthTokenResponse>.Fail(created.Errors);

        User newUser = created.Value;
        newUser.EmailConfirmed = true;
        newUser.IsActive = true;
        newUser.CreatedAtUtc = DateTimeOffset.UtcNow;
        newUser.CreatedBy = "External";

        IdentityResult result = await userManager.CreateAsync(newUser).ConfigureAwait(false);
        if (!result.Succeeded)
            return Fail<AuthTokenResponse>(result);

        if (await roleManager.RoleExistsAsync(IdentityConstant.Defaults.DefaultRole).ConfigureAwait(false))
        {
            await userManager
                .AddToRoleAsync(newUser, IdentityConstant.Defaults.DefaultRole)
                .ConfigureAwait(false);
        }

        IdentityResult loginResult = await userManager
            .AddLoginAsync(newUser, new UserLoginInfo(user.Provider, user.ProviderKey, user.Provider))
            .ConfigureAwait(false);

        if (!loginResult.Succeeded)
            return Fail<AuthTokenResponse>(loginResult);

        Result<AuthResponse> createdTokens = await tokenIssuer
            .IssueAsync(newUser, cancellationToken)
            .ConfigureAwait(false);

        return createdTokens.Map(response => response.ToTokenResponse());
    }

    private static string BuildUserName(ExternalUserInfo user)
    {
        string source = user.Email?.Split('@')[0]
            ?? user.DisplayName
            ?? $"external-{user.ProviderKey}";

        string sanitized = new string([.. source
            .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.')]);

        return string.IsNullOrWhiteSpace(sanitized)
            ? $"external-{Guid.NewGuid():N}"
            : sanitized;
    }

    private static Result<T> Fail<T>(IdentityResult result) =>
        Result<T>.Fail(result.Errors
            .Select(error => IdentityOutcome.Failure.IdentityError(error.Code, error.Description))
            .ToList());
}
