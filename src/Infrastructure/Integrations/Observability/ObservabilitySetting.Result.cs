namespace Infrastructure.Integrations.Observability;

public static class ObservabilitySettingResult
{
    public static class Failure
    {
        public static Error CorrelationHeaderEmpty =>
            Error.Validation(
                code: "Observability.CorrelationHeader.Empty",
                message: "Correlation header name must not be empty.");

        public static Error CorrelationHeaderInvalid =>
            Error.Validation(
                code: "Observability.CorrelationHeader.Invalid",
                message: "Correlation header name contains invalid characters.");

        public static Error ServiceNameEmpty =>
            Error.Validation(
                code: "Observability.ServiceName.Empty",
                message: "ServiceName must not be empty.");
    }
}
