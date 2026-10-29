namespace Application.Common.Security.Authentication.Tokens;

/// <summary>Failures produced by token configuration, issuance, rotation, and blacklisting.</summary>
public static class TokenOutcome
{
    /// <summary>Failure error constants for token services.</summary>
    public static class Failure
    {
        // ----- JWT configuration -----

        /// <summary>Error returned when the signing key is missing.</summary>
        public static Error SigningKeyRequired => Error.Validation(
            code: "Token.SigningKey.Required",
            message: "JWT signing key must be specified.");

        /// <summary>Error returned when the signing key is too short.</summary>
        public static Error SigningKeyTooShort => Error.Validation(
            code: "Token.SigningKey.TooShort",
            message:
                $"JWT signing key must contain at least {TokenConstant.Limits.MinSigningKeyLength} characters.");

        /// <summary>Error returned when the issuer is missing.</summary>
        public static Error IssuerRequired => Error.Validation(
            code: "Token.Issuer.Required",
            message: "JWT issuer must be specified.");

        /// <summary>Error returned when the audience is missing.</summary>
        public static Error AudienceRequired => Error.Validation(
            code: "Token.Audience.Required",
            message: "JWT audience must be specified.");

        /// <summary>Error returned when the access-token lifetime is out of range.</summary>
        public static Error AccessTokenLifetimeInvalid => Error.Validation(
            code: "Token.AccessToken.LifetimeInvalid",
            message:
                $"JWT access-token lifetime must be between {TokenConstant.Limits.MinAccessTokenMinutes} and {TokenConstant.Limits.MaxAccessTokenMinutes} minutes.");

        /// <summary>Error returned when the clock skew is out of range.</summary>
        public static Error ClockSkewInvalid => Error.Validation(
            code: "Token.ClockSkew.Invalid",
            message:
                $"JWT clock skew must be between 0 and {TokenConstant.Limits.MaxClockSkewSeconds} seconds.");

        // ----- Refresh-token configuration -----

        /// <summary>Error returned when the refresh-token lifetime is out of range.</summary>
        public static Error RefreshTokenLifetimeInvalid => Error.Validation(
            code: "Token.RefreshToken.LifetimeInvalid",
            message:
                $"Refresh-token lifetime must be between {TokenConstant.Limits.MinRefreshTokenDays} and {TokenConstant.Limits.MaxRefreshTokenDays} days.");

        /// <summary>Error returned when the refresh-token byte length is out of range.</summary>
        public static Error RefreshTokenLengthInvalid => Error.Validation(
            code: "Token.RefreshToken.LengthInvalid",
            message:
                $"Refresh-token byte length must be between {TokenConstant.Limits.MinRefreshTokenBytes} and {TokenConstant.Limits.MaxRefreshTokenBytes}.");

        /// <summary>Error returned when the refresh-token prefix is missing.</summary>
        public static Error RefreshTokenPrefixRequired => Error.Validation(
            code: "Token.RefreshToken.PrefixRequired",
            message: "Refresh-token prefix must be specified.");

        /// <summary>Error returned when the blacklist key prefix is missing.</summary>
        public static Error BlacklistPrefixRequired => Error.Validation(
            code: "Token.Blacklist.PrefixRequired",
            message: "Token blacklist key prefix must be specified.");

        // ----- Request validation -----

        /// <summary>Error returned when the user identifier is missing.</summary>
        public static Error UserIdRequired => Error.BadRequest(
            code: "Token.UserId.Required",
            message: "Token user identifier must be specified.");

        /// <summary>Error returned when the user identifier is too long.</summary>
        public static Error UserIdTooLong => Error.BadRequest(
            code: "Token.UserId.TooLong",
            message:
                $"Token user identifier cannot exceed {TokenConstant.Limits.MaxUserIdLength} characters.");

        /// <summary>Error returned when the display name is too long.</summary>
        public static Error UserNameTooLong => Error.BadRequest(
            code: "Token.UserName.TooLong",
            message:
                $"Token display name cannot exceed {TokenConstant.Limits.MaxUserNameLength} characters.");

        /// <summary>Error returned when the email address is too long.</summary>
        public static Error EmailTooLong => Error.BadRequest(
            code: "Token.Email.TooLong",
            message:
                $"Token email cannot exceed {TokenConstant.Limits.MaxEmailLength} characters.");

        /// <summary>Error returned when a custom claim key is missing.</summary>
        public static Error ClaimKeyRequired => Error.BadRequest(
            code: "Token.Claim.KeyRequired",
            message: "Token custom claim keys must not be empty.");

        /// <summary>Error returned when a custom claim key is too long.</summary>
        public static Error ClaimKeyTooLong => Error.BadRequest(
            code: "Token.Claim.KeyTooLong",
            message:
                $"Token custom claim keys cannot exceed {TokenConstant.Limits.MaxClaimKeyLength} characters.");

        /// <summary>Error returned when a custom claim key collides with a reserved claim.</summary>
        public static Error ClaimKeyReserved => Error.BadRequest(
            code: "Token.Claim.KeyReserved",
            message: "Token custom claim keys cannot overwrite reserved claims.");

        /// <summary>Error returned when the refresh token is missing.</summary>
        public static Error RefreshTokenRequired => Error.BadRequest(
            code: "Token.RefreshToken.Required",
            message: "Refresh token must be specified.");

        /// <summary>Error returned when the refresh token is too long.</summary>
        public static Error RefreshTokenTooLong => Error.BadRequest(
            code: "Token.RefreshToken.TooLong",
            message:
                $"Refresh token cannot exceed {TokenConstant.Limits.MaxRefreshTokenLength} characters.");

        /// <summary>Error returned when the token identifier is missing.</summary>
        public static Error JtiRequired => Error.BadRequest(
            code: "Token.Jti.Required",
            message: "Token identifier (jti) must be specified.");

        /// <summary>Error returned when the token identifier is too long.</summary>
        public static Error JtiTooLong => Error.BadRequest(
            code: "Token.Jti.TooLong",
            message:
                $"Token identifier (jti) cannot exceed {TokenConstant.Limits.MaxJtiLength} characters.");

        /// <summary>Error returned when the blacklist expiry is missing.</summary>
        public static Error BlacklistExpiryRequired => Error.BadRequest(
            code: "Token.Blacklist.ExpiryRequired",
            message: "Blacklisted token expiry must be specified.");

        // ----- Service failures -----

        /// <summary>Error returned when access-token generation fails.</summary>
        public static Error AccessTokenGenerationFailed => Error.Unexpected(
            code: "Token.AccessToken.GenerationFailed",
            message: "Failed to generate the access token.");

        /// <summary>Error returned when refresh-token generation fails.</summary>
        public static Error RefreshTokenGenerationFailed => Error.Unexpected(
            code: "Token.RefreshToken.GenerationFailed",
            message: "Failed to generate the refresh token.");

        /// <summary>Error returned when the blacklist store fails.</summary>
        public static Error BlacklistStoreFailure => Error.Unexpected(
            code: "Token.Blacklist.StoreFailure",
            message: "Failed to access the token blacklist store.");

        /// <summary>Error returned when refresh-token rotation fails.</summary>
        public static Error RotationFailed => Error.Unexpected(
            code: "Token.Rotation.Failed",
            message: "Failed to rotate the refresh token.");
    }
}
