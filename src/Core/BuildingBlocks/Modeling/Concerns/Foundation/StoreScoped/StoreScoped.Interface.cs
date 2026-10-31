namespace Core.BuildingBlocks.Modeling.Concerns.Foundation.StoreScoped;

/// <summary>Base concern for entities scoped to a store.</summary>
/// <typeparam name="TStoreKey">The type of the store identifier.</typeparam>
public interface IStoreScoped<TStoreKey>
{
    /// <summary>Gets or sets the store identifier for the entity.</summary>
    TStoreKey StoreId { get; set; }
}