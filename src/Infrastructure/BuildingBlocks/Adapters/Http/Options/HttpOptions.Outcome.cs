namespace Infrastructure.BuildingBlocks.Adapters.Http.Options;

public static class HttpOptionsOutcome
{
    public static class Failure
    {
        public static Error DefaultTimeoutSecondsOutOfRange =>
            Error.Validation(
                code: Codes.DefaultTimeoutSecondsOutOfRange,
                message: "DefaultTimeoutSeconds must be between 1 and 300.");

        public static Error ClientBaseAddressEmpty =>
            Error.Validation(
                code: Codes.ClientBaseAddressEmpty,
                message: "Client BaseAddress must not be empty.");

        public static Error ClientBaseAddressInvalid =>
            Error.Validation(
                code: Codes.ClientBaseAddressInvalid,
                message: "Client BaseAddress must be a valid absolute URI.");

        public static Error ClientTimeoutSecondsNegative =>
            Error.Validation(
                code: Codes.ClientTimeoutSecondsNegative,
                message: "Client TimeoutSeconds must be greater than or equal to 0.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string DefaultTimeoutSecondsOutOfRange = "HttpOptions.DefaultTimeoutSeconds.OutOfRange";
        public const string ClientBaseAddressEmpty = "HttpOptions.Client.BaseAddress.Empty";
        public const string ClientBaseAddressInvalid = "HttpOptions.Client.BaseAddress.Invalid";
        public const string ClientTimeoutSecondsNegative = "HttpOptions.Client.TimeoutSeconds.Negative";
    }
}
