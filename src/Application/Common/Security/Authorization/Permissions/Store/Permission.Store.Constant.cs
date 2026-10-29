namespace Application.Common.Security.Authorization.Permissions.Store;

/// <summary>Constraints for permission store operations.</summary>
public static class PermissionStoreConstant
{
    /// <summary>Operational limits enforced before a batch is persisted.</summary>
    public static class Constraints
    {
        /// <summary>The largest number of identifiers accepted by one batched write.</summary>
        public const int MaxPermissionsPerBatch = 1000;
    }
}
