namespace Infrastructure.Integrations.Http.Options;

public sealed class HttpOptions
{
    public const string SectionName = "Http";

    public int DefaultTimeoutSeconds { get; set; } = HttpConstant.Defaults.DefaultTimeoutSeconds;
    public bool AttachResiliencePipelineByDefault { get; set; } = HttpConstant.Defaults.AttachResiliencePipelineByDefault;
    public bool PropagateCorrelationId { get; set; } = HttpConstant.Defaults.PropagateCorrelationId;

    public Dictionary<string, NamedClientOptions> Clients { get; set; } = [];
}

public sealed class NamedClientOptions
{
    public string BaseAddress { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; }
    public bool AttachResiliencePipeline { get; set; } = HttpConstant.Defaults.AttachResiliencePipeline;
    public Dictionary<string, string> DefaultHeaders { get; set; } = [];
}
