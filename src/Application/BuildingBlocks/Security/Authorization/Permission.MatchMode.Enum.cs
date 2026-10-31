namespace Application.BuildingBlocks.Security.Authorization;

/// <summary>How a requirement combines multiple permission identifiers.</summary>
public enum PermissionMatchMode
{
    /// <summary>Every required permission must be held.</summary>
    All = 0,

    /// <summary>At least one required permission must be held.</summary>
    Any = 1,
}
