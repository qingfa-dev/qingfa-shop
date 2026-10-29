namespace Infrastructure.Integrations.Http.Options;

public static class HttpConstant
{
    public static class Defaults
    {
        public const int DefaultTimeoutSeconds = 30;
        public const bool AttachResiliencePipelineByDefault = true;
        public const bool PropagateCorrelationId = true;
        public const int TimeoutSeconds = 0;
        public const bool AttachResiliencePipeline = true;
    }

    public static class Constraints
    {
        public const int DefaultTimeoutSecondsMin = 1;
        public const int DefaultTimeoutSecondsMax = 300;
        public const int TimeoutSecondsMin = 0;
    }

    public static class Patterns
    {
    }
}
