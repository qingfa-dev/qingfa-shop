namespace Infrastructure.Integrations.Observability;

public static class ObservabilityConstant
{
    public static class Defaults
    {
        public const string ServiceName = "ReSys.Api";
        public const string ServiceVersion = "1.0.0";
        public const bool UseAspireOTLPExporter = true;
        public const string CorrelationHeader = "X-Correlation-Id";
        public const LogLevel MinimumLogLevel = LogLevel.Information;
        public static readonly string[] SensitiveHeaders =
            ["Authorization", "Cookie", "X-Api-Key"];
        public const bool ExposeDetailedReport = false;
    }

    public static class Constraints
    {
        public const int CorrelationHeaderMinLength = 1;
        public const int CorrelationHeaderMaxLength = 128;
        public const int ServiceNameMinLength = 1;
        public const int ServiceNameMaxLength = 256;
    }

    public static class Patterns
    {
        public const string CorrelationHeader = @"^[a-zA-Z0-9\-_]+$";
    }
}
