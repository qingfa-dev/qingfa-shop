namespace Application.BuildingBlocks.Ports.Notifications.Options.Channels;

/// <summary>Error definitions for channel configuration validation.</summary>
public static class ChannelOutcome
{
    /// <summary>Channel configuration error factories.</summary>
    public static class Failure
    {
        /// <summary>Channel configuration section is required.</summary>
        public static Error SectionRequired(string channelName) => Error.Validation(
            code: $"Channel.{channelName}.Section.Required",
            message: $"Channel {channelName} section name is required.");

        /// <summary>Channel name is required and must not be empty.</summary>
        public static Error NoEnabledProvider(string channelName) => Error.Validation(
            code: $"Channel.{channelName}.NoEnabledProvider",
            message: $"Channel {channelName} must have at least one enabled provider.");
    }
}
