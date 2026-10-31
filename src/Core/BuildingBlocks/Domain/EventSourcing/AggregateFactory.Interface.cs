namespace Core.BuildingBlocks.Domain.EventSourcing;

/// <summary>Creates an empty aggregate for rehydration.</summary>
/// <typeparam name="TAggregate">The aggregate type.</typeparam>
public interface IAggregateFactory<out TAggregate>
{
    /// <summary>Creates an empty aggregate with no events applied.</summary>
    TAggregate Create();
}