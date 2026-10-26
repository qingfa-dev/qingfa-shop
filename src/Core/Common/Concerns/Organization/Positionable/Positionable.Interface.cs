namespace Core.Common.Concerns.Organization.Positionable;

/// <summary>Base concern for position ordering.</summary>
public interface IPositionable
{
    #region Properties

    /// <summary>Gets or sets the position value.</summary>
    int Position { get; set; }

    #endregion
}