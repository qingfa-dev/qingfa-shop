namespace Infrastructure.Integrations.Storages.Processing;

public sealed partial class ImageProcessor
{
    internal static partial class Loggers
    {
        [LoggerMessage(
            EventId = 290,
            Level = LogLevel.Debug,
            Message = "Image processing started — target {Width}x{Height}, mode {Mode}, maintainAspectRatio={MaintainAspectRatio}")]
        public static partial void LogProcessingStarted(
            ILogger logger,
            int? width,
            int? height,
            ProcessingResizeMode mode,
            bool maintainAspectRatio);

        [LoggerMessage(
            EventId = 291,
            Level = LogLevel.Debug,
            Message = "Image processing completed — {SourceWidth}x{SourceHeight} → {TargetWidth}x{TargetHeight}, format={Format}")]
        public static partial void LogProcessingCompleted(
            ILogger logger,
            int sourceWidth,
            int sourceHeight,
            int targetWidth,
            int targetHeight,
            string format);

        [LoggerMessage(
            EventId = 292,
            Level = LogLevel.Warning,
            Message = "Image processing failed: {Reason}")]
        public static partial void LogProcessingFailed(
            ILogger logger,
            string reason);
    }
}
