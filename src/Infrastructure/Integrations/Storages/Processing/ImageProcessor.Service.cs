using Application.Common.Integrations.Storages.Models;

using SkiaSharp;

namespace Infrastructure.Integrations.Storages.Processing;

/// <summary>Processes images using SkiaSharp — supports resize (fit/fill/stretch), aspect ratio maintenance, and format conversion.</summary>
// Invariant: Returns original stream when no transformations requested; dimensions must be positive; unsupported formats fall back to source or JPEG.
// Boundary: Processor → SkiaSharp — image processing library boundary; all decoding/encoding handled by SkiaSharp codecs.
public sealed partial class ImageProcessor(ILogger<ImageProcessor> logger) : IImageProcessor
{
    /// <summary>Resizes and/or converts an image stream according to upload options.</summary>
    // Contract: pre=inputStream!=null && options!=null, post=return.IsSuccess implies valid image stream or original, throws=never
    public Task<Result<Stream>> ProcessAsync(
        Stream inputStream,
        UploadOptions options,
        CancellationToken ct = default)
    {
        int? width = options.ResizeWidth;
        int? height = options.ResizeHeight;

        // Guard: no transformations requested — return original stream unchanged
        if (width is null && height is null && options.OutputFormat is null)
            return Task.FromResult(Result<Stream>.Ok(inputStream));

        // Transform: map ResizeMode enum to processing mode for SkiaSharp operations
        ProcessingResizeMode mode = options.ResizeMode switch
        {
            ResizeMode.Fit => ProcessingResizeMode.Fit,
            ResizeMode.Fill => ProcessingResizeMode.Fill,
            ResizeMode.Stretch => ProcessingResizeMode.Stretch,
            _ => ProcessingResizeMode.Stretch
        };

        bool maintainAspectRatio = options.MaintainAspectRatio;
        string? outputFormat = options.OutputFormat;

        // Validate: dimensions must be positive if specified
        if ((width is not null && width <= 0) || (height is not null && height <= 0))
            return Task.FromResult<Result<Stream>>(ImageProcessorResult.Failure.InvalidDimensions(width, height));

        Loggers.LogProcessingStarted(logger, width, height, mode, maintainAspectRatio);

        try
        {
            SKEncodedImageFormat? sourceEncodedFormat;
            SKBitmap source;

            // Decode: open SkiaSharp codec from input stream — validates image format
            using (var codec = SKCodec.Create(inputStream))
            {
                if (codec is null)
                {
                    Loggers.LogProcessingFailed(logger, "Input stream does not contain a valid image.");
                    return Task.FromResult<Result<Stream>>(ImageProcessorResult.Failure.InvalidImage);
                }

                sourceEncodedFormat = codec.EncodedFormat;
                source = SKBitmap.Decode(codec);
            }

            // Validate: decoded bitmap must not be null
            if (source is null)
            {
                Loggers.LogProcessingFailed(logger, "Input stream does not contain a valid image.");
                return Task.FromResult<Result<Stream>>(ImageProcessorResult.Failure.InvalidImage);
            }

            using (source)
            {
                int sourceWidth = source.Width;
                int sourceHeight = source.Height;

                // Compute: derive missing dimension from the other to maintain proportions
                int targetWidth = width ?? (int)Math.Round(height!.Value * (double)sourceWidth / sourceHeight);
                int targetHeight = height ?? (int)Math.Round(width!.Value * (double)sourceHeight / sourceWidth);

                // Call: resolve encoder for output format — fall back to source or JPEG
                Func<SKImage, SKData>? encoder = ResolveEncoder(outputFormat, sourceEncodedFormat);

                if (encoder is null)
                {
                    Loggers.LogProcessingFailed(logger, $"Unsupported output format '{outputFormat}'.");
                    return Task.FromResult<Result<Stream>>(ImageProcessorResult.Failure.UnsupportedFormat(outputFormat!));
                }

                SKBitmap target;
                SKRect destRect;

                // Compute: determine target bitmap dimensions and destination rect based on resize mode
                if (!maintainAspectRatio || mode == ProcessingResizeMode.Stretch)
                {
                    target = new SKBitmap(targetWidth, targetHeight);
                    destRect = new SKRect(0, 0, targetWidth, targetHeight);
                }
                else if (mode == ProcessingResizeMode.Fit)
                {
                    float scale = Math.Min((float)targetWidth / sourceWidth, (float)targetHeight / sourceHeight);
                    int fitW = Math.Max(1, (int)Math.Round(sourceWidth * scale));
                    int fitH = Math.Max(1, (int)Math.Round(sourceHeight * scale));
                    target = new SKBitmap(fitW, fitH);
                    destRect = new SKRect(0, 0, fitW, fitH);
                }
                else
                {
                    float scale = Math.Max((float)targetWidth / sourceWidth, (float)targetHeight / sourceHeight);
                    float drawW = sourceWidth * scale;
                    float drawH = sourceHeight * scale;
                    float drawX = (targetWidth - drawW) / 2;
                    float drawY = (targetHeight - drawH) / 2;
                    target = new SKBitmap(targetWidth, targetHeight);
                    destRect = new SKRect(drawX, drawY, drawX + drawW, drawY + drawH);
                }

                using (target)
                using (var canvas = new SKCanvas(target))
                {
                    // Compute: clear canvas when fill mode used — transparent background for overlay
                    if (mode != ProcessingResizeMode.Fit || maintainAspectRatio == false)
                        canvas.Clear(SKColors.Transparent);

                    // Call: draw source image onto target canvas with resampling
                    using (var image = SKImage.FromBitmap(source))
                    {
                        canvas.DrawImage(image, destRect, Constants.DefaultResampleOptions);
                    }

                    canvas.Flush();

                    // Transform: encode target bitmap to output format and wrap in memory stream
                    using SKImage targetImage = SKImage.FromBitmap(target);
                    using SKData encoded = encoder(targetImage);

                    var outputStream = new MemoryStream();
                    encoded.SaveTo(outputStream);
                    outputStream.Position = 0;

                    Loggers.LogProcessingCompleted(logger, sourceWidth, sourceHeight, target.Width, target.Height, outputFormat ?? Constants.DefaultFormatLabel);

                    return Task.FromResult(Result<Stream>.Ok(outputStream));
                }
            }
        }
        catch (Exception ex)
        {
            // Catch: SkiaSharp processing error — return InvalidImage failure
            Loggers.LogProcessingFailed(logger, ex.Message);
            return Task.FromResult(
                Result<Stream>.Fail(ImageProcessorResult.Failure.InvalidImage));
        }
    }

    // Compute: resolve SKData encoder for output format — falls back to source format then JPEG
    private static Func<SKImage, SKData>? ResolveEncoder(string? outputFormat, SKEncodedImageFormat? sourceFormat)
    {
        if (outputFormat is not null)
            return Constants.FormatEncoders.TryGetValue(outputFormat, out Func<SKImage, SKData>? factory) ? factory : null;

        if (sourceFormat is null)
            return Constants.FallbackEncoder;

        // Transform: map SkiaSharp format enum to string key for encoder lookup
        string? formatKey = sourceFormat switch
        {
            SKEncodedImageFormat.Jpeg => "jpeg",
            SKEncodedImageFormat.Png => "png",
            SKEncodedImageFormat.Webp => "webp",
            _ => null
        };

        if (formatKey is null || !Constants.FormatEncoders.TryGetValue(formatKey, out Func<SKImage, SKData>? srcFactory))
            return Constants.FallbackEncoder;

        return srcFactory;
    }
}
