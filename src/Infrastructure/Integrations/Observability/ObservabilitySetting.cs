namespace Infrastructure.Integrations.Observability;

public sealed class ObservabilitySetting
{
    public const string SectionName = "Observability";

    public string ServiceName { get; set; } = ObservabilityConstant.Defaults.ServiceName;
    public string ServiceVersion { get; set; } = ObservabilityConstant.Defaults.ServiceVersion;
    public bool UseAspireOTLPExporter { get; set; } = ObservabilityConstant.Defaults.UseAspireOTLPExporter;
    public string CorrelationHeader { get; set; } = ObservabilityConstant.Defaults.CorrelationHeader;
    public LogLevel MinimumLogLevel { get; set; } = ObservabilityConstant.Defaults.MinimumLogLevel;
    public string[] SensitiveHeaders { get; set; } = ObservabilityConstant.Defaults.SensitiveHeaders;
    public bool ExposeDetailedReport { get; set; } = ObservabilityConstant.Defaults.ExposeDetailedReport;
}
