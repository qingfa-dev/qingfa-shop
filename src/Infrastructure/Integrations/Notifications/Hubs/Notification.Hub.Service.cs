using Application.Common.Integrations.Notifications.Models;

namespace Infrastructure.Integrations.Notifications.Hubs;

/// <summary>Orchestrates delivery across multiple notification providers with priority-based fallback — tries each provider sequentially until one succeeds.</summary>
// Invariant: Providers are filtered by channel and ordered by priority; first successful delivery short-circuits remaining providers.
// Boundary: Hub → INotificationProvider — orchestrates across all registered providers; never accesses delivery infrastructure directly.
public sealed partial class NotificationHub(
    IEnumerable<INotificationProvider> providers,
    ILogger<NotificationHub> logger)
    : INotificationHub
{
    /// <summary>Delivers a notification message by iterating active providers for the channel in priority order until one succeeds.</summary>
    // Contract: pre=message!=null, post=return.IsSuccess if any provider succeeded, throws=never
    public async Task<Result> SendAsync(
        NotificationMessage message,
        CancellationToken ct = default)
    {
        // Filter: active providers matching the message delivery channel, ordered by priority for fallback
        var activeProviders = providers
            .Where(p => p.IsEnabled)
            .Where(p => p.Channel == message.Channel)
            .OrderBy(p => p.Priority)
            .ToList();

        if (activeProviders.Count == 0)
        {
            // Log: no active providers configured for this channel
            Loggers.LogNoActiveProviders(logger, message.Channel);
            return NotificationHubResult.Failure.NoProvidersConfigured(
                    message.Channel);
        }

        var notificationRecipient = message.Recipient.Name ?? message.Recipient.Identifier;
        // Fallback: try each provider sequentially until one succeeds
        foreach (INotificationProvider? provider in activeProviders)
        {
            // Log: attempting provider delivery
            Loggers.LogAttemptingToSend(
                logger,
                provider.Name,
                provider.Channel,
                provider.Priority,
                notificationRecipient);

            // Call: delegate fully-prepared message to the provider (module boundary: Hub → Provider)
            Result result = await provider.SendAsync(message, ct);

            // Validate: delivery succeeded — return immediately, skip remaining providers
            if (result.IsSuccess)
            {
                Loggers.LogSendSuccess(
                    logger,
                    provider.Name,
                    provider.Channel,
                    notificationRecipient);

                return Result.Ok();
            }

            // Log: provider failed, try next in fallback chain
            Loggers.LogProviderFailed(
                logger,
                provider.Name,
                provider.Channel,
                string.Join("; ", result.Errors.Select(error => error.Message)));
        }

        // Log: all providers exhausted for this channel
        Loggers.LogAllProvidersFailed(
            logger,
            message.Channel,
            notificationRecipient);

        return
            NotificationHubResult.Failure.AllProvidersFailed(
                message.Channel);
    }
}