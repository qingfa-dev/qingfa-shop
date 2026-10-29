namespace Application.Common.Messaging.Bus;

public interface IEventBus
{
    Task<Result> PublishAsync<T>(
        T integrationEvent,
        CancellationToken cancellationToken = default)
        where T : class, IIntegrationEvent;
}
