namespace Application.BuildingBlocks.Ports.Notifications.Options.Channels;

/// <summary>Error definitions for channel configuration validation.</summary>
public static class ChannelOutcome
{
    /// <summary>Builds the stable codes for channel configuration failures.</summary>
    public static class Codes
    {
        /// <summary>Code for a missing channel section.</summary>
        public static string SectionRequired(string channelName) =>
            $"Channel.{channelName}.Section.Required";

        /// <summary>Code for a channel without an enabled provider.</summary>
        public static string NoEnabledProvider(string channelName) =>
            $"Channel.{channelName}.NoEnabledProvider";
    }

    /// <summary>Channel configuration error factories.</summary>
    public static class Failure
    {
        /// <summary>Channel configuration section is required.</summary>
        public static Error SectionRequired(string channelName) => Error.Validation(
            code: Codes.SectionRequired(channelName),
            message: $"Channel {channelName} section name is required.");

        /// <summary>Channel name is required and must not be empty.</summary>
        public static Error NoEnabledProvider(string channelName) => Error.Validation(
            code: Codes.NoEnabledProvider(channelName),
            message: $"Channel {channelName} must have at least one enabled provider.");
    }
}
