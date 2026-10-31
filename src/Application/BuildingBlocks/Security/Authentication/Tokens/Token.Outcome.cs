namespace Application.BuildingBlocks.Security.Authentication.Tokens;

/// <summary>Failures produced by token configuration, issuance, rotation, and blacklisting.</summary>
public static class TokenOutcome
{
    /// <summary>Failure error constants for token services.</summary>
    public static class Failure
    {
        // ----- JWT configuration -----

        /// <summary>Error returned when the signing key is missing.</summary>
        public static Error SigningKeyRequired => Error.Validation(
            code: Codes.SigningKeyRequired,
            message: "JWT signing key must be specified.");

        /// <summary>Error returned when the signing key is too short.</summary>
        public static Error SigningKeyTooShort => Error.Validation(
            code: Codes.SigningKeyTooShort,
            message:
                $"JWT signing key must contain at least {TokenConstant.Limits.MinSigningKeyLength} characters.");

        /// <summary>Error returned when the issuer is missing.</summary>
        public static Error IssuerRequired => Error.Validation(
            code: Codes.IssuerRequired,
            message: "JWT issuer must be specified.");

        /// <summary>Error returned when the audience is missing.</summary>
        public static Error AudienceRequired => Error.Validation(
            code: Codes.AudienceRequired,
            message: "JWT audience must be specified.");

        /// <summary>Error returned when the access-token lifetime is out of range.</summary>
        public static Error AccessTokenLifetimeInvalid => Error.Validation(
            code: Codes.AccessTokenLifetimeInvalid,
            message:
                $"JWT access-token lifetime must be between {TokenConstant.Limits.MinAccessTokenMinutes} and {TokenConstant.Limits.MaxAccessTokenMinutes} minutes.");

        /// <summary>Error returned when the clock skew is out of range.</summary>
        public static Error ClockSkewInvalid => Error.Validation(
            code: Codes.ClockSkewInvalid,
            message:
                $"JWT clock skew must be between 0 and {TokenConstant.Limits.MaxClockSkewSeconds} seconds.");

        // ----- Refresh-token configuration -----

        /// <summary>Error returned when the refresh-token lifetime is out of range.</summary>
        public static Error RefreshTokenLifetimeInvalid => Error.Validation(
            code: Codes.RefreshTokenLifetimeInvalid,
            message:
                $"Refresh-token lifetime must be between {TokenConstant.Limits.MinRefreshTokenDays} and {TokenConstant.Limits.MaxRefreshTokenDays} days.");

        /// <summary>Error returned when the refresh-token byte length is out of range.</summary>
        public static Error RefreshTokenLengthInvalid => Error.Validation(
            code: Codes.RefreshTokenLengthInvalid,
            message:
                $"Refresh-token byte length must be between {TokenConstant.Limits.MinRefreshTokenBytes} and {TokenConstant.Limits.MaxRefreshTokenBytes}.");

        /// <summary>Error returned when the refresh-token prefix is missing.</summary>
        public static Error RefreshTokenPrefixRequired => Error.Validation(
            code: Codes.RefreshTokenPrefixRequired,
            message: "Refresh-token prefix must be specified.");

        /// <summary>Error returned when the blacklist key prefix is missing.</summary>
        public static Error BlacklistPrefixRequired => Error.Validation(
            code: Codes.BlacklistPrefixRequired,
            message: "Token blacklist key prefix must be specified.");

        // ----- Request validation -----

        /// <summary>Error returned when the user identifier is missing.</summary>
        public static Error UserIdRequired => Error.BadRequest(
            code: Codes.UserIdRequired,
            message: "Token user identifier must be specified.");

        /// <summary>Error returned when the user identifier is too long.</summary>
        public static Error UserIdTooLong => Error.BadRequest(
            code: Codes.UserIdTooLong,
            message:
                $"Token user identifier cannot exceed {TokenConstant.Limits.MaxUserIdLength} characters.");

        /// <summary>Error returned when the display name is too long.</summary>
        public static Error UserNameTooLong => Error.BadRequest(
            code: Codes.UserNameTooLong,
            message:
                $"Token display name cannot exceed {TokenConstant.Limits.MaxUserNameLength} characters.");

        /// <summary>Error returned when the email address is too long.</summary>
        public static Error EmailTooLong => Error.BadRequest(
            code: Codes.EmailTooLong,
            message:
                $"Token email cannot exceed {TokenConstant.Limits.MaxEmailLength} characters.");

        /// <summary>Error returned when a custom claim key is missing.</summary>
        public static Error ClaimKeyRequired => Error.BadRequest(
            code: Codes.ClaimKeyRequired,
            message: "Token custom claim keys must not be empty.");

        /// <summary>Error returned when a custom claim key is too long.</summary>
        public static Error ClaimKeyTooLong => Error.BadRequest(
            code: Codes.ClaimKeyTooLong,
            message:
                $"Token custom claim keys cannot exceed {TokenConstant.Limits.MaxClaimKeyLength} characters.");

        /// <summary>Error returned when a custom claim key collides with a reserved claim.</summary>
        public static Error ClaimKeyReserved => Error.BadRequest(
            code: Codes.ClaimKeyReserved,
            message: "Token custom claim keys cannot overwrite reserved claims.");

        /// <summary>Error returned when the refresh token is missing.</summary>
        public static Error RefreshTokenRequired => Error.BadRequest(
            code: Codes.RefreshTokenRequired,
            message: "Refresh token must be specified.");

        /// <summary>Error returned when the refresh token is too long.</summary>
        public static Error RefreshTokenTooLong => Error.BadRequest(
            code: Codes.RefreshTokenTooLong,
            message:
                $"Refresh token cannot exceed {TokenConstant.Limits.MaxRefreshTokenLength} characters.");

        /// <summary>Error returned when the token identifier is missing.</summary>
        public static Error JtiRequired => Error.BadRequest(
            code: Codes.JtiRequired,
            message: "Token identifier (jti) must be specified.");

        /// <summary>Error returned when the token identifier is too long.</summary>
        public static Error JtiTooLong => Error.BadRequest(
            code: Codes.JtiTooLong,
            message:
                $"Token identifier (jti) cannot exceed {TokenConstant.Limits.MaxJtiLength} characters.");

        /// <summary>Error returned when the blacklist expiry is missing.</summary>
        public static Error BlacklistExpiryRequired => Error.BadRequest(
            code: Codes.BlacklistExpiryRequired,
            message: "Blacklisted token expiry must be specified.");

        // ----- Service failures -----

        /// <summary>Error returned when access-token generation fails.</summary>
        public static Error AccessTokenGenerationFailed => Error.Unexpected(
            code: Codes.AccessTokenGenerationFailed,
            message: "Failed to generate the access token.");

        /// <summary>Error returned when refresh-token generation fails.</summary>
        public static Error RefreshTokenGenerationFailed => Error.Unexpected(
            code: Codes.RefreshTokenGenerationFailed,
            message: "Failed to generate the refresh token.");

        /// <summary>Error returned when the blacklist store fails.</summary>
        public static Error BlacklistStoreFailure => Error.Unexpected(
            code: Codes.BlacklistStoreFailure,
            message: "Failed to access the token blacklist store.");

        /// <summary>Error returned when refresh-token rotation fails.</summary>
        public static Error RotationFailed => Error.Unexpected(
            code: Codes.RotationFailed,
            message: "Failed to rotate the refresh token.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string SigningKeyRequired = "Token.SigningKey.Required";
        public const string SigningKeyTooShort = "Token.SigningKey.TooShort";
        public const string IssuerRequired = "Token.Issuer.Required";
        public const string AudienceRequired = "Token.Audience.Required";
        public const string AccessTokenLifetimeInvalid = "Token.AccessToken.LifetimeInvalid";
        public const string ClockSkewInvalid = "Token.ClockSkew.Invalid";
        public const string RefreshTokenLifetimeInvalid = "Token.RefreshToken.LifetimeInvalid";
        public const string RefreshTokenLengthInvalid = "Token.RefreshToken.LengthInvalid";
        public const string RefreshTokenPrefixRequired = "Token.RefreshToken.PrefixRequired";
        public const string BlacklistPrefixRequired = "Token.Blacklist.PrefixRequired";
        public const string UserIdRequired = "Token.UserId.Required";
        public const string UserIdTooLong = "Token.UserId.TooLong";
        public const string UserNameTooLong = "Token.UserName.TooLong";
        public const string EmailTooLong = "Token.Email.TooLong";
        public const string ClaimKeyRequired = "Token.Claim.KeyRequired";
        public const string ClaimKeyTooLong = "Token.Claim.KeyTooLong";
        public const string ClaimKeyReserved = "Token.Claim.KeyReserved";
        public const string RefreshTokenRequired = "Token.RefreshToken.Required";
        public const string RefreshTokenTooLong = "Token.RefreshToken.TooLong";
        public const string JtiRequired = "Token.Jti.Required";
        public const string JtiTooLong = "Token.Jti.TooLong";
        public const string BlacklistExpiryRequired = "Token.Blacklist.ExpiryRequired";
        public const string AccessTokenGenerationFailed = "Token.AccessToken.GenerationFailed";
        public const string RefreshTokenGenerationFailed = "Token.RefreshToken.GenerationFailed";
        public const string BlacklistStoreFailure = "Token.Blacklist.StoreFailure";
        public const string RotationFailed = "Token.Rotation.Failed";
    }
}
