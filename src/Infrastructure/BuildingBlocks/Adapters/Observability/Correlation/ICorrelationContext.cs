namespace Infrastructure.BuildingBlocks.Adapters.Observability.Correlation;

public interface ICorrelationContext
{
    string? CorrelationId { get; set; }
}
