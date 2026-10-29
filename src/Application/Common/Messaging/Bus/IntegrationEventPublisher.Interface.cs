namespace Application.Common.Messaging.Bus;

public interface IIntegrationEventPublisher
{
    Task<Result> PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default);
}
