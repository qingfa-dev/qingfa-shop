namespace Core.BuildingBlocks.Modeling.Concerns.Organization.Categorizable;

/// <summary>Base concern for category management.</summary>
/// <typeparam name="TCategory">The category type.</typeparam>
public interface ICategorizable<TCategory>
{
    #region Properties

    /// <summary>Gets the categories assigned to the entity.</summary>
    ICollection<TCategory> Categories { get; }

    #endregion
}