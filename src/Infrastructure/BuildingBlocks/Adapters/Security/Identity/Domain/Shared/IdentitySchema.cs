namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Shared;

/// <summary>Physical schema and table names used by ASP.NET Core Identity.</summary>
public static class IdentitySchema
{
    /// <summary>The PostgreSQL schema that holds identity tables.</summary>
    public const string Name = "identity";

    /// <summary>Table names in the identity schema.</summary>
    public static class TableNames
    {
        /// <summary>User accounts.</summary>
        public const string Users = "users";

        /// <summary>Roles.</summary>
        public const string Roles = "roles";

        /// <summary>User-to-role links.</summary>
        public const string UserRoles = "user_roles";

        /// <summary>User claims.</summary>
        public const string UserClaims = "user_claims";

        /// <summary>External logins.</summary>
        public const string UserLogins = "user_logins";

        /// <summary>Identity security tokens.</summary>
        public const string UserTokens = "user_tokens";

        /// <summary>Role claims.</summary>
        public const string RoleClaims = "role_claims";

        /// <summary>Refresh tokens.</summary>
        public const string RefreshTokens = "refresh_tokens";

        /// <summary>User passkeys.</summary>
        public const string Passkeys = "passkeys";

        /// <summary>Owned passkey payload.</summary>
        public const string PasskeyData = "passkey_data";
    }
}
