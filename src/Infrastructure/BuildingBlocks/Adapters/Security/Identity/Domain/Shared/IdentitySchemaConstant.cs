namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Shared;

/// <summary>Column limits shared by the identity entity configurations.</summary>
public static class IdentitySchemaConstant
{
    /// <summary>Audit columns recorded on identity rows.</summary>
    public static class Columns
    {
        /// <summary>Actor columns (created-by and modified-by) store the ASP.NET Identity key.</summary>
        public static class Actor
        {
            /// <summary>Maximum length of the actor columns.</summary>
            public const int MaxLength = 450;
        }
    }
}
