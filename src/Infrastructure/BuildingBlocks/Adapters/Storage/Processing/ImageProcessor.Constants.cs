using SkiaSharp;

namespace Infrastructure.BuildingBlocks.Adapters.Storage.Processing;

public sealed partial class ImageProcessor
{
    internal static class Constants
    {
        internal const string DefaultFormatLabel = "auto";

        internal static readonly SKSamplingOptions DefaultResampleOptions = new(SKFilterMode.Linear, SKMipmapMode.Linear);

        internal static readonly Dictionary<string, Func<SKImage, SKData>> FormatEncoders = new(StringComparer.OrdinalIgnoreCase)
        {
            ["jpeg"] = img => img.Encode(SKEncodedImageFormat.Jpeg, 85),
            ["jpg"] = img => img.Encode(SKEncodedImageFormat.Jpeg, 85),
            ["png"] = img => img.Encode(SKEncodedImageFormat.Png, 100),
            ["webp"] = img => img.Encode(SKEncodedImageFormat.Webp, 85),
        };

        internal static readonly Func<SKImage, SKData> FallbackEncoder = img => img.Encode(SKEncodedImageFormat.Jpeg, 85);
    }
}
