namespace Infrastructure.Integrations.Http.Options;

public static class HttpOptionsResult
{
    public static class Failure
    {
        public static Error DefaultTimeoutSecondsOutOfRange =>
            Error.Validation(
                code: "HttpOptions.DefaultTimeoutSeconds.OutOfRange",
                message: "DefaultTimeoutSeconds must be between 1 and 300.");

        public static Error ClientBaseAddressEmpty =>
            Error.Validation(
                code: "HttpOptions.Client.BaseAddress.Empty",
                message: "Client BaseAddress must not be empty.");

        public static Error ClientBaseAddressInvalid =>
            Error.Validation(
                code: "HttpOptions.Client.BaseAddress.Invalid",
                message: "Client BaseAddress must be a valid absolute URI.");

        public static Error ClientTimeoutSecondsNegative =>
            Error.Validation(
                code: "HttpOptions.Client.TimeoutSeconds.Negative",
                message: "Client TimeoutSeconds must be greater than or equal to 0.");
    }
}
