namespace Infrastructure.Integrations.Observability.Correlation;

public interface ICorrelationContext
{
    string? CorrelationId { get; set; }
}
