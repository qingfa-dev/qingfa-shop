namespace Application.BuildingBlocks.Security.Authorization;

/// <summary>Shared authorization constants referenced by the domain and the host.</summary>
public static class AuthorizationConstant
{
    /// <summary>Default values used when the host does not override authorization behavior.</summary>
    public static class Defaults
    {
        /// <summary>The role that bypasses permission evaluation.</summary>
        public const string AdminRole = "Admin";
    }
}
