namespace Core.Common.Concerns.Organization.Hierarchical;

/// <summary>Base concern for hierarchical parent-child relationships.</summary>
/// <typeparam name="TKey">The key type for the parent identifier.</typeparam>
public interface IHierarchical<TKey>
    where TKey : struct
{
    #region Properties

    /// <summary>Gets or sets the parent identifier; <c>null</c> for root entities.</summary>
    TKey? ParentId { get; set; }

    #endregion
}