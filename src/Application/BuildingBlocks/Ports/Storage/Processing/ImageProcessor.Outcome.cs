using System.Globalization;

namespace Application.BuildingBlocks.Ports.Storage.Processing;

public static class ImageProcessorOutcome
{
    public static class Failure
    {
        public static Error InvalidDimensions(int? width, int? height)
            => Error.Validation(
                Codes.InvalidDimensions,
                $"Invalid image dimensions: width={width?.ToString(CultureInfo.InvariantCulture) ?? "null"}, height={height?.ToString(CultureInfo.InvariantCulture) ?? "null"}");

        public static Error InvalidImage
            => Error.Unexpected(
                Codes.InvalidImage,
                "The input stream does not contain a valid image.");

        public static Error UnsupportedFormat(string format)
            => Error.Validation(
                Codes.UnsupportedFormat,
                $"Unsupported output format '{format}'.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string InvalidDimensions = "Processing.InvalidDimensions";
        public const string InvalidImage = "Processing.InvalidImage";
        public const string UnsupportedFormat = "Processing.UnsupportedFormat";
    }
}
