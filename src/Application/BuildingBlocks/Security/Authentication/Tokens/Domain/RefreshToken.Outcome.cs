namespace Application.BuildingBlocks.Security.Authentication.Tokens.Domain;

/// <summary>Result constants for refresh token operations.</summary>
public static class RefreshTokenOutcome
{
    /// <summary>Failure error constants for refresh tokens.</summary>
    public static class Failure
    {
        /// <summary>Error returned when the refresh token is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Refresh token must be specified.");

        /// <summary>Error returned when the refresh token cannot be found.</summary>
        public static Error NotFound => Error.NotFound(
            code: Codes.NotFound,
            message: "Refresh token not found.");

        /// <summary>Error returned when the refresh token has expired.</summary>
        public static Error Expired => Error.Unauthorized(
            code: Codes.Expired,
            message: "Refresh token has expired.");

        /// <summary>Error returned when the refresh token has been revoked.</summary>
        public static Error Revoked => Error.Unauthorized(
            code: Codes.Revoked,
            message: "Refresh token has been revoked.");

        /// <summary>Error returned when reuse of a rotated token indicates theft.</summary>
        public static Error TheftDetected => Error.Unauthorized(
            code: Codes.TheftDetected,
            message: "Token reuse detected. All user tokens have been revoked.");

        /// <summary>Error returned when the token hash is missing.</summary>
        public static Error TokenHashRequired => Error.BadRequest(
            code: Codes.TokenHashRequired,
            message: "Refresh token hash must be specified.");

        /// <summary>Error returned when the token hash exceeds its maximum length.</summary>
        public static Error TokenHashTooLong => Error.BadRequest(
            code: Codes.TokenHashTooLong,
            message:
                $"Refresh token hash cannot exceed {RefreshTokenConstant.Constraints.TokenHash.MaxLength} characters.");

        /// <summary>Error returned when the owning user identifier is missing.</summary>
        public static Error UserIdRequired => Error.BadRequest(
            code: Codes.UserIdRequired,
            message: "Refresh token user identifier must be specified.");

        /// <summary>Error returned when the owning user identifier exceeds its maximum length.</summary>
        public static Error UserIdTooLong => Error.BadRequest(
            code: Codes.UserIdTooLong,
            message:
                $"Refresh token user identifier cannot exceed {RefreshTokenConstant.Constraints.UserId.MaxLength} characters.");

        /// <summary>Error returned when the token family identifier is missing.</summary>
        public static Error TokenFamilyIdRequired => Error.BadRequest(
            code: Codes.TokenFamilyIdRequired,
            message: "Refresh token family identifier must be specified.");

        /// <summary>Error returned when the creation date is missing.</summary>
        public static Error CreatedAtRequired => Error.BadRequest(
            code: Codes.CreatedAtRequired,
            message: "Refresh token creation date must be specified.");

        /// <summary>Error returned when the expiry date is missing.</summary>
        public static Error ExpiresAtRequired => Error.BadRequest(
            code: Codes.ExpiresAtRequired,
            message: "Refresh token expiry date must be specified.");

        /// <summary>Error returned when the expiry does not follow the creation date.</summary>
        public static Error ExpiresAtBeforeCreated => Error.BadRequest(
            code: Codes.ExpiresAtBeforeCreated,
            message: "Refresh token expiry must be later than its creation date.");

        /// <summary>Error returned when the revocation date precedes the creation date.</summary>
        public static Error RevokedAtBeforeCreated => Error.BadRequest(
            code: Codes.RevokedAtBeforeCreated,
            message: "Refresh token revocation date cannot precede its creation date.");

        /// <summary>Error returned when a revoked token has no revocation reason.</summary>
        public static Error RevocationReasonRequired => Error.BadRequest(
            code: Codes.RevocationReasonRequired,
            message: "Refresh token revocation reason must be specified when the token is revoked.");

        /// <summary>Error returned when the revocation reason is not a defined value.</summary>
        public static Error RevocationReasonInvalid => Error.BadRequest(
            code: Codes.RevocationReasonInvalid,
            message: "Refresh token revocation reason is not a defined value.");

        /// <summary>Error returned when a reason is supplied without a revocation date.</summary>
        public static Error RevocationReasonWithoutRevocation => Error.BadRequest(
            code: Codes.RevocationReasonWithoutRevocation,
            message: "Refresh token revocation reason requires a revocation date.");

        /// <summary>Error returned when rotation has no replacement token.</summary>
        public static Error ReplacementRequired => Error.BadRequest(
            code: Codes.ReplacementRequired,
            message: "Replacement refresh token must be specified.");

        /// <summary>Error returned when a token is used as its own replacement.</summary>
        public static Error ReplacementSameToken => Error.Conflict(
            code: Codes.ReplacementSameToken,
            message: "A refresh token cannot replace itself.");

        /// <summary>Error returned when the replacement belongs to another token family.</summary>
        public static Error ReplacementFamilyMismatch => Error.Conflict(
            code: Codes.ReplacementFamilyMismatch,
            message: "Replacement refresh token must belong to the same token family.");

        /// <summary>Error returned when the replacement belongs to another user.</summary>
        public static Error ReplacementUserMismatch => Error.Conflict(
            code: Codes.ReplacementUserMismatch,
            message: "Replacement refresh token must belong to the same user.");

        /// <summary>Error returned when the replacement is expired or revoked.</summary>
        public static Error ReplacementNotActive => Error.Conflict(
            code: Codes.ReplacementNotActive,
            message: "Replacement refresh token must be active.");

        /// <summary>Error returned when token generation fails.</summary>
        public static Error GenerationFailed => Error.Unexpected(
            code: Codes.GenerationFailed,
            message: "Failed to generate refresh token.");

        /// <summary>Error returned when token rotation fails.</summary>
        public static Error RotationFailed => Error.Unexpected(
            code: Codes.RotationFailed,
            message: "Failed to rotate refresh token.");

        /// <summary>Error returned when token revocation fails.</summary>
        public static Error RevocationFailed => Error.Unexpected(
            code: Codes.RevocationFailed,
            message: "Failed to revoke refresh token.");

        /// <summary>Error returned when reuse detection fails.</summary>
        public static Error TokenTheftDetectorFailure => Error.Unexpected(
            code: Codes.TokenTheftDetectorFailure,
            message: "Failed to detect token reuse due to infrastructure error.");

        /// <summary>Error returned when the token store cannot be accessed.</summary>
        public static Error StoreFailure => Error.Unexpected(
            code: Codes.StoreFailure,
            message: "Failed to access refresh token store.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "RefreshToken.Entity.Required";
        public const string NotFound = "RefreshToken.NotFound";
        public const string Expired = "RefreshToken.Expired";
        public const string Revoked = "RefreshToken.Revoked";
        public const string TheftDetected = "RefreshToken.TheftDetected";
        public const string TokenHashRequired = "RefreshToken.TokenHash.Required";
        public const string TokenHashTooLong = "RefreshToken.TokenHash.TooLong";
        public const string UserIdRequired = "RefreshToken.UserId.Required";
        public const string UserIdTooLong = "RefreshToken.UserId.TooLong";
        public const string TokenFamilyIdRequired = "RefreshToken.TokenFamilyId.Required";
        public const string CreatedAtRequired = "RefreshToken.CreatedAt.Required";
        public const string ExpiresAtRequired = "RefreshToken.ExpiresAt.Required";
        public const string ExpiresAtBeforeCreated = "RefreshToken.ExpiresAt.BeforeCreated";
        public const string RevokedAtBeforeCreated = "RefreshToken.RevokedAt.BeforeCreated";
        public const string RevocationReasonRequired = "RefreshToken.RevocationReason.Required";
        public const string RevocationReasonInvalid = "RefreshToken.RevocationReason.Invalid";
        public const string RevocationReasonWithoutRevocation = "RefreshToken.RevocationReason.WithoutRevocation";
        public const string ReplacementRequired = "RefreshToken.Replacement.Required";
        public const string ReplacementSameToken = "RefreshToken.Replacement.SameToken";
        public const string ReplacementFamilyMismatch = "RefreshToken.Replacement.FamilyMismatch";
        public const string ReplacementUserMismatch = "RefreshToken.Replacement.UserMismatch";
        public const string ReplacementNotActive = "RefreshToken.Replacement.NotActive";
        public const string GenerationFailed = "RefreshToken.GenerationFailed";
        public const string RotationFailed = "RefreshToken.RotationFailed";
        public const string RevocationFailed = "RefreshToken.RevocationFailed";
        public const string TokenTheftDetectorFailure = "RefreshToken.TokenTheftDetectorFailure";
        public const string StoreFailure = "RefreshToken.StoreFailure";
    }
}
