namespace Core.BuildingBlocks.Modeling.Concerns.Content.MediaAttachable;

/// <summary>Represents an entity that supports attached media.</summary>
/// <typeparam name="TMedia">The type of the media.</typeparam>
public interface IMediaAttachable<TMedia>
{
    /// <summary>Gets the collection of attached media.</summary>
    ICollection<TMedia> Media { get; }
}