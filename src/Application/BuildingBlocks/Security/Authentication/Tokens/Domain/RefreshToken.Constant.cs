namespace Application.BuildingBlocks.Security.Authentication.Tokens.Domain;

/// <summary>Constants and limits for <see cref="IRefreshToken"/>.</summary>
public static class RefreshTokenConstant
{
    /// <summary>Constraint values for <see cref="IRefreshToken"/>.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for the stored token hash.</summary>
        public static class TokenHash
        {
            /// <summary>Minimum length for a token hash.</summary>
            public const int MinLength = 1;

            /// <summary>Maximum length for a token hash.</summary>
            public const int MaxLength = 500;
        }

        /// <summary>Length constraints for the owning user identifier.</summary>
        public static class UserId
        {
            /// <summary>Maximum length for a user identifier.</summary>
            public const int MaxLength = 450;
        }
    }
}
