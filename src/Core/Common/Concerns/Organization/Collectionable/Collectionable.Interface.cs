namespace Core.Common.Concerns.Organization.Collectionable;

/// <summary>Base concern for collection management.</summary>
/// <typeparam name="TCollection">The collection type.</typeparam>
public interface ICollectionable<TCollection>
{
    #region Properties

    /// <summary>Gets the collections assigned to the entity.</summary>
    ICollection<TCollection> Collections { get; }

    #endregion
}