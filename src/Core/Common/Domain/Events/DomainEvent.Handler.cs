using MediatR;

namespace Core.Common.Domain.Events;

public interface IDomainEventHandler : INotificationHandler<IDomainEvent>;