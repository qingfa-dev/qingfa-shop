namespace Application.Common.Security.Authentication.Identity;

/// <summary>Validates identity API requests before they reach the Identity stores.</summary>
public static class IdentityValidator
{
    /// <summary>Validates a registration request.</summary>
    public static Result<RegisterRequest> Validate(RegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Result<RegisterRequest>.Success(request)
            .Ensure(value => ValidateUserName(value.UserName))
            .Ensure(value => ValidateEmail(value.Email))
            .Ensure(value => ValidatePassword(value.Password))
            .Ensure(
                value => value.FirstName is null ||
                    value.FirstName.Length <= IdentityConstant.Constraints.MaxFirstNameLength,
                IdentityOutcome.Failure.PasswordInvalid("First name is too long."))
            .Ensure(
                value => value.LastName is null ||
                    value.LastName.Length <= IdentityConstant.Constraints.MaxLastNameLength,
                IdentityOutcome.Failure.PasswordInvalid("Last name is too long."));
    }

    /// <summary>Validates a local login request.</summary>
    public static Result<LoginRequest> Validate(LoginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Result<LoginRequest>.Success(request)
            .Ensure(
                value => !string.IsNullOrWhiteSpace(value.Identifier),
                IdentityOutcome.Failure.InvalidCredentials)
            .Ensure(
                value => !string.IsNullOrWhiteSpace(value.Password),
                IdentityOutcome.Failure.InvalidCredentials);
    }

    /// <summary>Validates a refresh request.</summary>
    public static Result<RefreshRequest> Validate(RefreshRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Result<RefreshRequest>.Success(request)
            .Ensure(
                value => !string.IsNullOrWhiteSpace(value.RefreshToken),
                IdentityOutcome.Failure.RefreshTokenRequired);
    }

    /// <summary>Validates an email confirmation request.</summary>
    public static Result<ConfirmEmailRequest> Validate(ConfirmEmailRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Result<ConfirmEmailRequest>.Success(request)
            .Ensure(value => Guid.TryParse(value.UserId, out _), IdentityOutcome.Failure.InvalidToken)
            .Ensure(value => !string.IsNullOrWhiteSpace(value.Token), IdentityOutcome.Failure.InvalidToken);
    }

    /// <summary>Validates a confirmation-resend request.</summary>
    public static Result<ResendConfirmationEmailRequest> Validate(ResendConfirmationEmailRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Result<ResendConfirmationEmailRequest>.Success(request)
            .Ensure(value => ValidateEmail(value.Email));
    }

    /// <summary>Validates a forgot-password request.</summary>
    public static Result<ForgotPasswordRequest> Validate(ForgotPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Result<ForgotPasswordRequest>.Success(request)
            .Ensure(value => ValidateEmail(value.Email));
    }

    /// <summary>Validates a password reset request.</summary>
    public static Result<ResetPasswordRequest> Validate(ResetPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Result<ResetPasswordRequest>.Success(request)
            .Ensure(value => ValidateEmail(value.Email))
            .Ensure(value => !string.IsNullOrWhiteSpace(value.Token), IdentityOutcome.Failure.InvalidToken)
            .Ensure(value => ValidatePassword(value.NewPassword));
    }

    /// <summary>Validates a profile update request.</summary>
    public static Result<UpdateInfoRequest> Validate(UpdateInfoRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<UpdateInfoRequest> result = Result<UpdateInfoRequest>.Success(request)
            .Ensure(value => value.Email is null
                ? Result.Ok()
                : ValidateEmail(value.Email))
            .Ensure(
                value => value.FirstName is null ||
                    value.FirstName.Length <= IdentityConstant.Constraints.MaxFirstNameLength,
                IdentityOutcome.Failure.OperationFailed("First name is too long."))
            .Ensure(
                value => value.LastName is null ||
                    value.LastName.Length <= IdentityConstant.Constraints.MaxLastNameLength,
                IdentityOutcome.Failure.OperationFailed("Last name is too long."));

        return request.NewPassword is null
            ? result
            : result
                .Ensure(
                    value => !string.IsNullOrWhiteSpace(value.OldPassword),
                    IdentityOutcome.Failure.CurrentPasswordRequired)
                .Ensure(value => ValidatePassword(value.NewPassword!));
    }

    /// <summary>Validates a two-factor request.</summary>
    public static Result<TwoFactorRequest> Validate(TwoFactorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Result<TwoFactorRequest>.Success(request)
            .Ensure(
                value => value.Enable is not true || !string.IsNullOrWhiteSpace(value.TwoFactorCode),
                IdentityOutcome.Failure.TwoFactorRequired);
    }

    /// <summary>Validates normalized external identity data.</summary>
    public static Result<ExternalUserInfo> Validate(ExternalUserInfo user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return Result<ExternalUserInfo>.Success(user)
            .Ensure(
                value => !string.IsNullOrWhiteSpace(value.Provider),
                IdentityOutcome.Failure.ExternalLoginFailed)
            .Ensure(
                value => !string.IsNullOrWhiteSpace(value.ProviderKey),
                IdentityOutcome.Failure.ExternalLoginFailed)
            .Ensure(value => value.Email is null
                ? Result.Ok()
                : ValidateEmail(value.Email));
    }

    /// <summary>Validates a username.</summary>
    public static Result ValidateUserName(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return Result.Fail(IdentityOutcome.Failure.InvalidCredentials);

        if (userName.Length < IdentityConstant.Constraints.MinUserNameLength ||
            userName.Length > IdentityConstant.Constraints.MaxUserNameLength)
        {
            return Result.Fail(IdentityOutcome.Failure.OperationFailed("Username length is invalid."));
        }

        return Result.Ok();
    }

    /// <summary>Validates an email address.</summary>
    public static Result ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Fail(IdentityOutcome.Failure.InvalidCredentials);

        if (email.Length > IdentityConstant.Constraints.MaxEmailLength)
            return Result.Fail(IdentityOutcome.Failure.OperationFailed("Email is too long."));

        int at = email.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at < email.Length - 1
            ? Result.Ok()
            : Result.Fail(IdentityOutcome.Failure.InvalidCredentials);
    }

    /// <summary>Validates a password against the application policy.</summary>
    public static Result ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return Result.Fail(IdentityOutcome.Failure.PasswordInvalid("Password is required."));

        if (password.Length < IdentityConstant.Constraints.MinPasswordLength)
            return Result.Fail(IdentityOutcome.Failure.PasswordInvalid("Password is too short."));

        if (password.Length > IdentityConstant.Constraints.MaxPasswordLength)
            return Result.Fail(IdentityOutcome.Failure.PasswordInvalid("Password is too long."));

        return Result.Ok();
    }
}
