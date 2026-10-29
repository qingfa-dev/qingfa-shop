namespace Application.Common.Security.Authentication.Tokens;

/// <summary>Constants shared by token issuance, refresh, rotation, and blacklisting.</summary>
public static class TokenConstant
{
    /// <summary>Configuration section names.</summary>
    public static class Sections
    {
        /// <summary>Section bound to <see cref="Options.JwtTokenOption"/>.</summary>
        public const string Jwt = "Jwt";

        /// <summary>Section bound to <see cref="Options.RefreshTokenOption"/>.</summary>
        public const string RefreshToken = "RefreshToken";

        /// <summary>Section bound to <see cref="Options.TokenBlacklistOption"/>.</summary>
        public const string Blacklist = "TokenBlacklist";
    }

    /// <summary>Claim names emitted into access tokens.</summary>
    public static class Claims
    {
        /// <summary>The subject claim.</summary>
        public const string Subject = "sub";

        /// <summary>The display-name claim.</summary>
        public const string Name = "name";

        /// <summary>The email claim.</summary>
        public const string Email = "email";

        /// <summary>The tenant claim.</summary>
        public const string TenantId = "tenant_id";

        /// <summary>The token identifier claim.</summary>
        public const string Jti = "jti";

        /// <summary>The role claim.</summary>
        public const string Role = "role";

        /// <summary>The issued-at claim.</summary>
        public const string IssuedAt = "iat";
    }

    /// <summary>Hard limits for token configuration and requests.</summary>
    public static class Limits
    {
        /// <summary>Minimum symmetric signing-key length in characters.</summary>
        public const int MinSigningKeyLength = 32;

        /// <summary>Minimum access-token lifetime in minutes.</summary>
        public const int MinAccessTokenMinutes = 1;

        /// <summary>Maximum access-token lifetime in minutes.</summary>
        public const int MaxAccessTokenMinutes = 1440;

        /// <summary>Maximum accepted clock skew in seconds.</summary>
        public const int MaxClockSkewSeconds = 300;

        /// <summary>Minimum refresh-token lifetime in days.</summary>
        public const int MinRefreshTokenDays = 1;

        /// <summary>Maximum refresh-token lifetime in days.</summary>
        public const int MaxRefreshTokenDays = 365;

        /// <summary>Minimum random byte count for a refresh token.</summary>
        public const int MinRefreshTokenBytes = 16;

        /// <summary>Maximum random byte count for a refresh token.</summary>
        public const int MaxRefreshTokenBytes = 64;

        /// <summary>Maximum accepted refresh-token length in characters.</summary>
        public const int MaxRefreshTokenLength = 512;

        /// <summary>Maximum accepted token-identifier length.</summary>
        public const int MaxJtiLength = 128;

        /// <summary>Maximum accepted user-identifier length.</summary>
        public const int MaxUserIdLength = 450;

        /// <summary>Maximum accepted display-name length.</summary>
        public const int MaxUserNameLength = 256;

        /// <summary>Maximum accepted email length.</summary>
        public const int MaxEmailLength = 320;

        /// <summary>Maximum accepted custom claim-key length.</summary>
        public const int MaxClaimKeyLength = 128;
    }

    /// <summary>Default values used when configuration omits a setting.</summary>
    public static class Defaults
    {
        /// <summary>Default access-token lifetime in minutes.</summary>
        public const int AccessTokenMinutes = 15;

        /// <summary>Default clock skew in seconds.</summary>
        public const int ClockSkewSeconds = 30;

        /// <summary>Default refresh-token lifetime in days.</summary>
        public const int RefreshTokenDays = 7;

        /// <summary>Default random byte count for a refresh token.</summary>
        public const int RefreshTokenBytes = 32;

        /// <summary>Default prefix that identifies opaque refresh tokens.</summary>
        public const string RefreshTokenPrefix = "rt_";

        /// <summary>Default prefix for blacklist cache keys.</summary>
        public const string BlacklistKeyPrefix = "auth:blacklist:";
    }

    /// <summary>Token serialization values.</summary>
    public static class Scheme
    {
        /// <summary>The bearer token type returned to callers.</summary>
        public const string Bearer = "Bearer";

        /// <summary>The HMAC-SHA256 algorithm name.</summary>
        public const string HmacSha256 = "HS256";
    }
}
