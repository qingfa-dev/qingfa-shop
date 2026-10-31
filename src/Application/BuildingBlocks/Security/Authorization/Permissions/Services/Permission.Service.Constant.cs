namespace Application.BuildingBlocks.Security.Authorization.Permissions.Services;

/// <summary>Constant constraints and error code patterns for PermissionService operations.</summary>
public static class PermissionServiceConstant
{
    // Const: Operational limits for batch permission operations.
    public static class Constraints
    {
        public const int MaxPermissionsPerBatch = 100;
    }

    // Const: Error code prefix patterns for permission service failures.
    public static class Patterns
    {
        public const string ErrorCodePrefix = "PermissionService.";
    }
}
