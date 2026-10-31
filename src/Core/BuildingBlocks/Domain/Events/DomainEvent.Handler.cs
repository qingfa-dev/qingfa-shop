using MediatR;

namespace Core.BuildingBlocks.Domain.Events;

public interface IDomainEventHandler : INotificationHandler<IDomainEvent>;