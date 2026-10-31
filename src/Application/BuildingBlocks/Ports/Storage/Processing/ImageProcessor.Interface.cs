using Application.BuildingBlocks.Ports.Storage.Models;

namespace Application.BuildingBlocks.Ports.Storage.Processing;

/// <summary>Server-side image processing service for resize and format conversion.</summary>
public interface IImageProcessor
{
    /// <summary>
    /// Process an image stream — resize, change format, or both.
    /// </summary>
    /// <param name="inputStream">Source image data stream.</param>
    /// <param name="options">Upload options containing resize dimensions, mode, aspect ratio, and output format.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="Result{T}"/> containing the processed image stream on success.</returns>
    Task<Result<Stream>> ProcessAsync(
        Stream inputStream,
        UploadOptions options,
        CancellationToken ct = default);
}
