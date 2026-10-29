using Application.Common.Events;

using Core.Common.Domain.Aggregates;
using Core.Common.Domain.Events;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Publishes the domain events raised by tracked aggregates after a successful save.
/// </summary>
/// <remarks>
/// <para>
/// Only aggregates that implement <see cref="IAggregate{TEvent}"/> for
/// <see cref="IDomainEvent"/> are considered. Their event lists are cleared before dispatch,
/// so a handler that saves the same context cannot republish the events that are already
/// in flight.
/// </para>
/// <para>
/// A failed save never publishes: <see cref="SavedChanges"/> is only invoked once the store
/// accepted the changes. Events raised by a failed unit of work stay on the aggregate and
/// are published only if that aggregate is saved again.
/// </para>
/// <para>
/// Dispatch happens immediately after the save completes. A durable outbox is required when
/// events must be published only after a surrounding transaction commits; that is a separate
/// persistence capability.
/// </para>
/// </remarks>
public sealed class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IDomainEventPublisher _publisher;
    private readonly ILogger _logger;

    /// <summary>Creates a domain-event interceptor.</summary>
    /// <param name="publisher">The publisher that dispatches the collected events.</param>
    /// <param name="logger">The logger for publication diagnostics.</param>
    public DomainEventInterceptor(
        IDomainEventPublisher publisher,
        ILogger<DomainEventInterceptor>? logger = null)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? NullLogger<DomainEventInterceptor>.Instance;
    }

    /// <inheritdoc />
    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        // Flush: the synchronous save path has no async continuation to await.
        PublishAsync(eventData.Context, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await PublishAsync(eventData.Context, cancellationToken).ConfigureAwait(false);

        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears and dispatches the events of every tracked aggregate root.
    /// </summary>
    /// <param name="context">The context that just completed a save.</param>
    /// <param name="cancellationToken">A token that cancels dispatch between handlers.</param>
    private async Task PublishAsync(
        DbContext? context,
        CancellationToken cancellationToken)
    {
        if (context is null)
            return;

        List<IAggregate<IDomainEvent>> aggregates = context.ChangeTracker
            .Entries<IAggregate<IDomainEvent>>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.Events.Count > 0)
            .ToList();

        if (aggregates.Count == 0)
            return;

        List<IDomainEvent> events = aggregates
            .SelectMany(aggregate => aggregate.Events)
            .ToList();

        // Clear: remove the events before dispatch so a re-entrant save cannot publish them twice.
        foreach (IAggregate<IDomainEvent> aggregate in aggregates)
            aggregate.RemoveAllEvents();

        DomainEventInterceptorLoggers.Publishing(_logger, events.Count);

        Result result = await _publisher
            .PublishAsync(events, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            Error error = result.Errors[0];
            DomainEventInterceptorLoggers.PublishFailed(_logger, events.Count, error.Code, error.Message);

            // Escalate: the caller must see that post-save dispatch failed.
            result.ThrowIfFailure();
        }
    }
}
