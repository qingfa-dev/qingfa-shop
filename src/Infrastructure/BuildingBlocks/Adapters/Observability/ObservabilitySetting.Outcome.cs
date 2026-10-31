namespace Infrastructure.BuildingBlocks.Adapters.Observability;

public static class ObservabilitySettingOutcome
{
    public static class Failure
    {
        public static Error CorrelationHeaderEmpty =>
            Error.Validation(
                code: Codes.CorrelationHeaderEmpty,
                message: "Correlation header name must not be empty.");

        public static Error CorrelationHeaderInvalid =>
            Error.Validation(
                code: Codes.CorrelationHeaderInvalid,
                message: "Correlation header name contains invalid characters.");

        public static Error ServiceNameEmpty =>
            Error.Validation(
                code: Codes.ServiceNameEmpty,
                message: "ServiceName must not be empty.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string CorrelationHeaderEmpty = "Observability.CorrelationHeader.Empty";
        public const string CorrelationHeaderInvalid = "Observability.CorrelationHeader.Invalid";
        public const string ServiceNameEmpty = "Observability.ServiceName.Empty";
    }
}
