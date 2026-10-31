namespace Application.BuildingBlocks.Security.Authentication.Tokens.Options;

/// <summary>Configuration for issuing and validating JWT access tokens.</summary>
public sealed class JwtTokenOption
{
    /// <summary>Gets or sets the symmetric signing key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the token issuer.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Gets or sets the intended audience.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Gets or sets the access-token lifetime in minutes.</summary>
    public int AccessTokenMinutes { get; set; } = TokenConstant.Defaults.AccessTokenMinutes;

    /// <summary>Gets or sets the accepted clock skew in seconds.</summary>
    public int ClockSkewSeconds { get; set; } = TokenConstant.Defaults.ClockSkewSeconds;
}

/// <summary>Configuration for opaque refresh-token generation.</summary>
public sealed class RefreshTokenOption
{
    /// <summary>Gets or sets the refresh-token lifetime in days.</summary>
    public int LifetimeDays { get; set; } = TokenConstant.Defaults.RefreshTokenDays;

    /// <summary>Gets or sets the random byte count used for each token.</summary>
    public int TokenByteLength { get; set; } = TokenConstant.Defaults.RefreshTokenBytes;

    /// <summary>Gets or sets the prefix that identifies refresh tokens.</summary>
    public string TokenPrefix { get; set; } = TokenConstant.Defaults.RefreshTokenPrefix;
}

/// <summary>Configuration for the access-token blacklist.</summary>
public sealed class TokenBlacklistOption
{
    /// <summary>Gets or sets the prefix used for blacklist store keys.</summary>
    public string KeyPrefix { get; set; } = TokenConstant.Defaults.BlacklistKeyPrefix;

    /// <summary>Gets or sets a value indicating whether blacklist checks are enabled.</summary>
    public bool Enabled { get; set; } = true;
}
