using System.Globalization;

namespace Application.Common.Integrations.Storages.Processing;

public static class ImageProcessorResult
{
    public static class Failure
    {
        public static Error InvalidDimensions(int? width, int? height)
            => Error.Validation(
                "Processing.InvalidDimensions",
                $"Invalid image dimensions: width={width?.ToString(CultureInfo.InvariantCulture) ?? "null"}, height={height?.ToString(CultureInfo.InvariantCulture) ?? "null"}");

        public static Error InvalidImage
            => Error.Unexpected(
                "Processing.InvalidImage",
                "The input stream does not contain a valid image.");

        public static Error UnsupportedFormat(string format)
            => Error.Validation(
                "Processing.UnsupportedFormat",
                $"Unsupported output format '{format}'.");
    }
}
