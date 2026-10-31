namespace Application.BuildingBlocks.Ports.Storage.Processing;

/// <summary>Strategies for resizing an image to fit within target dimensions.</summary>
public enum ProcessingResizeMode
{
    /// <summary>Scale the image to fit entirely within the target bounds while preserving aspect ratio.
    /// Empty space (letterboxing) may appear if the source aspect ratio differs from the target.</summary>
    Fit,

    /// <summary>Scale the image to completely fill the target bounds while preserving aspect ratio.
    /// The image is cropped if the source aspect ratio differs from the target.</summary>
    Fill,

    /// <summary>Scale the image to exactly match the target dimensions, ignoring aspect ratio.</summary>
    Stretch
}
