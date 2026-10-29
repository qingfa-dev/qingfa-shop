using Application.Common.Security.Authentication.Identity;
using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Abstractions.Jwt;
using Application.Common.Security.Authentication.Tokens.Abstractions.Blacklist;
using Application.Common.Security.Authentication.Tokens.Abstractions.Refresh;
using Application.Common.Security.Authentication.Tokens.Abstractions.Rotation;
using Application.Common.Security.Authentication.Tokens.Domain;

using Infrastructure.Security.Identity.Domain.Roles;
using Infrastructure.Security.Identity.Domain.Users;

using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security.Identity.Services;

/// <summary>Full ASP.NET Core Identity application service backed by EF Core stores.</summary>
public sealed class IdentityService(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    SignInManager<User> signInManager,
    ITokenService tokenService,
    IRefreshTokenService refreshTokens,
    ITokenRotationService rotation,
    ITokenBlacklist blacklist,
    IIdentityEmailSender emailSender,
    IdentityTokenIssuer tokenIssuer,
    IdentityApiOption option,
    TimeProvider clock) : IIdentityService
{
    /// <inheritdoc />
    public async Task<Result<IdentityUserModel>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<RegisterRequest> validation = IdentityValidator.Validate(request);
        if (validation.IsFailure)
            return Result<IdentityUserModel>.Fail(validation.Errors);

        if (await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false) is not null)
            return Result<IdentityUserModel>.Fail(IdentityOutcome.Failure.EmailTaken);

        if (await userManager.FindByNameAsync(request.UserName).ConfigureAwait(false) is not null)
            return Result<IdentityUserModel>.Fail(IdentityOutcome.Failure.UserNameTaken);

        Result<User> created = User.Create(
            request.UserName,
            request.Email,
            request.FirstName ?? request.UserName,
            request.LastName);

        if (created.IsFailure)
            return Result<IdentityUserModel>.Fail(created.Errors);

        User user = created.Value;
        user.IsActive = true;
        user.CreatedAtUtc = clock.GetUtcNow();
        user.CreatedBy = "System";

        IdentityResult result = await userManager
            .CreateAsync(user, request.Password)
            .ConfigureAwait(false);

        if (!result.Succeeded)
            return Fail<IdentityUserModel>(result);

        if (!string.IsNullOrWhiteSpace(option.DefaultRole) &&
            await roleManager.RoleExistsAsync(option.DefaultRole).ConfigureAwait(false))
        {
            IdentityResult roleResult = await userManager
                .AddToRoleAsync(user, option.DefaultRole)
                .ConfigureAwait(false);

            if (!roleResult.Succeeded)
                return Fail<IdentityUserModel>(roleResult);
        }

        Result email = await SendConfirmationAsync(user, cancellationToken).ConfigureAwait(false);
        if (email.IsFailure)
            return Result<IdentityUserModel>.Fail(email.Errors);

        return Result<IdentityUserModel>.Ok(IdentityUserProjection.ToModel(user));
    }

    /// <inheritdoc />
    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<LoginRequest> validation = IdentityValidator.Validate(request);
        if (validation.IsFailure)
            return Result<AuthResponse>.Fail(validation.Errors);

        User? user = request.Identifier.Contains('@')
            ? await userManager.FindByEmailAsync(request.Identifier).ConfigureAwait(false)
            : await userManager.FindByNameAsync(request.Identifier).ConfigureAwait(false);

        if (user is null)
            return Result<AuthResponse>.Fail(IdentityOutcome.Failure.InvalidCredentials);

        if (!user.IsActive)
            return Result<AuthResponse>.Fail(IdentityOutcome.Failure.Inactive);

        if ((option.RequireConfirmedEmail || option.RequireConfirmedAccount) &&
            !user.EmailConfirmed)
        {
            return Result<AuthResponse>.Fail(IdentityOutcome.Failure.EmailNotConfirmed);
        }

        SignInResult signIn = await signInManager
            .CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true)
            .ConfigureAwait(false);

        if (signIn.RequiresTwoFactor)
        {
            if (!string.IsNullOrWhiteSpace(request.TwoFactorRecoveryCode))
            {
                signIn = await signInManager
                    .TwoFactorRecoveryCodeSignInAsync(request.TwoFactorRecoveryCode)
                    .ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(request.TwoFactorCode))
            {
                signIn = await signInManager
                    .TwoFactorAuthenticatorSignInAsync(
                        request.TwoFactorCode,
                        isPersistent: false,
                        rememberClient: false)
                    .ConfigureAwait(false);
            }
            else
            {
                return Result<AuthResponse>.Fail(IdentityOutcome.Failure.TwoFactorRequired);
            }
        }

        if (signIn.IsLockedOut)
            return Result<AuthResponse>.Fail(IdentityOutcome.Failure.LockedOut);

        if (signIn.IsNotAllowed)
            return Result<AuthResponse>.Fail(IdentityOutcome.Failure.NotAllowed);

        if (!signIn.Succeeded)
            return Result<AuthResponse>.Fail(IdentityOutcome.Failure.InvalidCredentials);

        return await tokenIssuer.IssueAsync(user, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<AuthResponse>> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<RefreshRequest> validation = IdentityValidator.Validate(request);
        if (validation.IsFailure)
            return Result<AuthResponse>.Fail(validation.Errors);

        Result<RefreshTokenIssue> rotated = await rotation
            .RotateAsync(request.RefreshToken, clock.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);

        if (rotated.IsFailure)
            return Result<AuthResponse>.Fail(rotated.Errors);

        User? user = await userManager
            .FindByIdAsync(rotated.Value.Entity.UserId)
            .ConfigureAwait(false);

        return user is null
            ? Result<AuthResponse>.Fail(IdentityOutcome.Failure.UserNotFound)
            : await CreateAuthResponseAsync(user, rotated.Value, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            Result<RefreshToken> found = await refreshTokens
                .FindAsync(request.RefreshToken, cancellationToken)
                .ConfigureAwait(false);

            if (found.IsSuccess)
            {
                await refreshTokens
                    .RevokeAsync(
                        found.Value,
                        clock.GetUtcNow(),
                        RefreshTokenRevocationReason.UserLogout,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.AccessTokenJti) &&
            request.AccessTokenExpiresAtUtc is not null)
        {
            Result blacklisted = await blacklist
                .AddAsync(
                    request.AccessTokenJti,
                    request.AccessTokenExpiresAtUtc.Value,
                    cancellationToken)
                .ConfigureAwait(false);

            if (blacklisted.IsFailure)
                return blacklisted;
        }

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<ConfirmEmailRequest> validation = IdentityValidator.Validate(request);
        if (validation.IsFailure)
            return validation;

        User? user = await userManager.FindByIdAsync(request.UserId).ConfigureAwait(false);
        if (user is null)
            return Result.Fail(IdentityOutcome.Failure.UserNotFound);

        IdentityResult result = await userManager
            .ConfirmEmailAsync(user, request.Token)
            .ConfigureAwait(false);

        return result.Succeeded ? Result.Ok() : Fail(result);
    }

    /// <inheritdoc />
    public async Task<Result> ResendConfirmationEmailAsync(
        ResendConfirmationEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<ResendConfirmationEmailRequest> validation = IdentityValidator.Validate(request);
        if (validation.IsFailure)
            return validation;

        User? user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        return user is null || user.EmailConfirmed
            ? Result.Ok()
            : await SendConfirmationAsync(user, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<ForgotPasswordRequest> validation = IdentityValidator.Validate(request);
        if (validation.IsFailure)
            return validation;

        User? user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null || !user.IsActive)
            return Result.Ok();

        string token = await userManager
            .GeneratePasswordResetTokenAsync(user)
            .ConfigureAwait(false);

        string link = BuildLink(option.ResetPasswordPath, user.Id, token);
        return await emailSender
            .SendPasswordResetAsync(user.Email ?? request.Email, link, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<ResetPasswordRequest> validation = IdentityValidator.Validate(request);
        if (validation.IsFailure)
            return validation;

        User? user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null)
            return Result.Fail(IdentityOutcome.Failure.InvalidToken);

        IdentityResult result = await userManager
            .ResetPasswordAsync(user, request.Token, request.NewPassword)
            .ConfigureAwait(false);

        return result.Succeeded ? Result.Ok() : Fail(result);
    }

    /// <inheritdoc />
    public async Task<Result<IdentityUserModel>> GetInfoAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        User? user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        return user is null
            ? Result<IdentityUserModel>.Fail(IdentityOutcome.Failure.UserNotFound)
            : Result<IdentityUserModel>.Ok(IdentityUserProjection.ToModel(user));
    }

    /// <inheritdoc />
    public async Task<Result<IdentityUserModel>> UpdateInfoAsync(
        Guid userId,
        UpdateInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<UpdateInfoRequest> validation = IdentityValidator.Validate(request);
        if (validation.IsFailure)
            return Result<IdentityUserModel>.Fail(validation.Errors);

        User? user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return Result<IdentityUserModel>.Fail(IdentityOutcome.Failure.UserNotFound);

        bool changed = false;

        if (!string.IsNullOrWhiteSpace(request.Email) &&
            !string.Equals(request.Email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            IdentityResult emailResult = await userManager
                .SetEmailAsync(user, request.Email)
                .ConfigureAwait(false);

            if (!emailResult.Succeeded)
                return Fail<IdentityUserModel>(emailResult);

            user.EmailConfirmed = false;
            Result confirmation = await SendConfirmationAsync(user, cancellationToken).ConfigureAwait(false);
            if (confirmation.IsFailure)
                return Result<IdentityUserModel>.Fail(confirmation.Errors);

            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            user.FirstName = request.FirstName.Trim();
            changed = true;
        }

        if (request.LastName is not null)
        {
            user.LastName = request.LastName.Trim();
            changed = true;
        }

        if (changed)
        {
            user.ModifiedAtUtc = clock.GetUtcNow();
            user.ModifiedBy = "System";
            IdentityResult update = await userManager.UpdateAsync(user).ConfigureAwait(false);
            if (!update.Succeeded)
                return Fail<IdentityUserModel>(update);
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            IdentityResult password = await userManager
                .ChangePasswordAsync(user, request.OldPassword!, request.NewPassword)
                .ConfigureAwait(false);

            if (!password.Succeeded)
            {
                return password.Errors.Any(error => error.Code == "PasswordMismatch")
                    ? Result<IdentityUserModel>.Fail(IdentityOutcome.Failure.CurrentPasswordInvalid)
                    : Fail<IdentityUserModel>(password);
            }
        }

        return Result<IdentityUserModel>.Ok(IdentityUserProjection.ToModel(user));
    }

    /// <inheritdoc />
    public async Task<Result<TwoFactorInfo>> GetTwoFactorInfoAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        User? user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return Result<TwoFactorInfo>.Fail(IdentityOutcome.Failure.UserNotFound);

        string? key = await userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user).ConfigureAwait(false);
            key = await userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
        }

        return Result<TwoFactorInfo>.Ok(new TwoFactorInfo(
            await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false),
            key,
            BuildAuthenticatorUri(user, key),
            []));
    }

    /// <inheritdoc />
    public async Task<Result<TwoFactorInfo>> UpdateTwoFactorAsync(
        Guid userId,
        TwoFactorRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<TwoFactorRequest> validation = IdentityValidator.Validate(request);
        if (validation.IsFailure)
            return Result<TwoFactorInfo>.Fail(validation.Errors);

        User? user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return Result<TwoFactorInfo>.Fail(IdentityOutcome.Failure.UserNotFound);

        IReadOnlyCollection<string> recoveryCodes = [];

        if (request.ResetSharedKey is true)
        {
            await userManager.ResetAuthenticatorKeyAsync(user).ConfigureAwait(false);
        }

        if (request.ResetRecoveryCodes is true)
        {
            IEnumerable<string>? codes = await userManager
                .GenerateNewTwoFactorRecoveryCodesAsync(
                    user,
                    IdentityConstant.Constraints.RecoveryCodeCount)
                .ConfigureAwait(false);

            recoveryCodes = codes is null ? [] : [.. codes];
        }

        if (request.Enable is true)
        {
            bool valid = await userManager
                .VerifyTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultAuthenticatorProvider,
                    request.TwoFactorCode!)
                .ConfigureAwait(false);

            if (!valid)
                return Result<TwoFactorInfo>.Fail(IdentityOutcome.Failure.TwoFactorInvalid);

            IdentityResult enabled = await userManager
                .SetTwoFactorEnabledAsync(user, true)
                .ConfigureAwait(false);

            if (!enabled.Succeeded)
                return Fail<TwoFactorInfo>(enabled);
        }
        else if (request.Enable is false)
        {
            IdentityResult disabled = await userManager
                .SetTwoFactorEnabledAsync(user, false)
                .ConfigureAwait(false);

            if (!disabled.Succeeded)
                return Fail<TwoFactorInfo>(disabled);
        }

        Result<TwoFactorInfo> info = await GetTwoFactorInfoAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return info.IsSuccess
            ? Result<TwoFactorInfo>.Ok(info.Value with { RecoveryCodes = recoveryCodes })
            : info;
    }

    private async Task<Result<AuthResponse>> CreateAuthResponseAsync(
        User user,
        RefreshTokenIssue refreshToken,
        CancellationToken cancellationToken)
    {
        IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        Result<AccessToken> accessToken = tokenService.CreateAccessToken(
            new AccessTokenRequest(
                user.Id.ToString(),
                user.UserName,
                user.Email,
                TenantId: null,
                Roles: [.. roles]));

        return accessToken.IsSuccess
            ? Result<AuthResponse>.Ok(
                new AuthResponse(
                    accessToken.Value,
                    refreshToken,
                    IdentityUserProjection.ToModel(user)))
            : Result<AuthResponse>.Fail(accessToken.Errors);
    }

    private async Task<Result> SendConfirmationAsync(
        User user,
        CancellationToken cancellationToken)
    {
        string token = await userManager
            .GenerateEmailConfirmationTokenAsync(user)
            .ConfigureAwait(false);

        string link = BuildLink(option.ConfirmationPath, user.Id, token);
        return await emailSender
            .SendConfirmationAsync(user.Email ?? string.Empty, link, cancellationToken)
            .ConfigureAwait(false);
    }

    private string BuildLink(string path, Guid userId, string token) =>
        $"{option.PublicBaseUrl.TrimEnd('/')}{path}?userId={userId}&token={Uri.EscapeDataString(token)}";

    private static string? BuildAuthenticatorUri(User user, string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(user.Email))
            return null;

        string issuer = "QingFa.Shop";
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(user.Email)}" +
            $"?secret={Uri.EscapeDataString(key)}&issuer={Uri.EscapeDataString(issuer)}&digits=6";
    }

    private static Result<T> Fail<T>(IdentityResult result) =>
        Result<T>.Fail(result.Errors
            .Select(error => IdentityOutcome.Failure.IdentityError(error.Code, error.Description))
            .ToList());

    private static Result Fail(IdentityResult result) =>
        Result.Fail(result.Errors
            .Select(error => IdentityOutcome.Failure.IdentityError(error.Code, error.Description))
            .ToList());
}
