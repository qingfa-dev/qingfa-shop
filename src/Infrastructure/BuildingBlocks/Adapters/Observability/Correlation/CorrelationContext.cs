namespace Infrastructure.BuildingBlocks.Adapters.Observability.Correlation;

internal sealed class CorrelationContext : ICorrelationContext
{
    public string? CorrelationId { get; set; }
}
