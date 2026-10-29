namespace Infrastructure.Integrations.Observability.Correlation;

internal sealed class CorrelationContext : ICorrelationContext
{
    public string? CorrelationId { get; set; }
}
