using Hangfire;
using Hangfire.States;

using Microsoft.Extensions.Options;

using Application.Common.Integrations.Notifications.Hubs;
using Application.Common.Integrations.Notifications.Models;
using Application.Common.Integrations.Notifications.Store;

namespace Infrastructure.Integrations.Notifications.Services;

/// <summary>Orchestrates notification delivery via template resolution, default application, content rendering, and dispatch to the notification hub — optionally via background jobs.</summary>
// Invariant: Every use case must have a registered template; background dispatch uses priority-based queues.
// Boundary: Service → NotificationHub | Hangfire — delegates delivery to hub; uses Hangfire for async dispatch.
public sealed partial class NotificationService(
    INotificationHub notificationHub,
    IBackgroundJobClient? jobClient,
    IOptions<NotificationSetting> options,
    ILogger<NotificationService> logger)
    : INotificationService
{
    /// <summary>Dispatches a notification — either via background job (if enabled) or synchronously through the delivery pipeline.</summary>
    // Contract: pre=message!=null, post=return.IsSuccess if delivered, throws=never
    public async Task<Result> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        // Validate: template must be registered for the requested use case before proceeding
        if (!NotificationStore.Templates.TryGetValue(message.UseCase, out NotificationTemplate? template))
        {
            return NotificationResult.Failure.TemplateNotFound(message.UseCase.ToString());
        }

        // Call: dispatch via Hangfire background job when enabled, for async delivery and retry
        if (options.Value.EnableBackgroundJobs)
        {
            if (jobClient is null)
            {
                return NotificationResult.Failure.BackgroundJobClientNotConfigured(message.UseCase.ToString());
            }

            var queue = template.Priority.ToQueueName();

            // Log: record hand-off to background worker system
            Loggers.LogEnqueuingNotificationJob(logger, message.UseCase, template.Priority, queue);

            // Trigger: create background job in appropriate priority queue (boundary: Service → Hangfire)
            jobClient.Create<INotificationService>(
                service => service.SendInternalAsync(message, ct),
                new EnqueuedState(queue));

            return Result.Ok();
        }

        return await SendInternalAsync(message, ct);
    }

    /// <summary>Applies defaults, renders content, and delivers notification through the hub pipeline.</summary>
    // Contract: pre=message!=null, post=return.IsSuccess if delivered, throws=never
    public async Task<Result> SendInternalAsync(NotificationMessage message, CancellationToken ct = default)
    {
        // Transform: fill missing system parameters with global defaults from configuration
        message = message.ApplyDefaults(options.Value);

        // Validate: confirm template exists during delivery phase (defensive check)
        if (!NotificationStore.Templates.TryGetValue(message.UseCase, out NotificationTemplate? template))
        {
            return NotificationResult.Failure.TemplateNotFound(message.UseCase.ToString());
        }

        // Log: trace the start of actual delivery processing
        Loggers.LogProcessingNotificationDelivery(logger, message.UseCase, message.Recipient.Identifier);

        // Transform: render template placeholders with provided context values into final content
        Result<NotificationContent> contentResult = message.MapContent();
        if (contentResult.IsFailure) return contentResult.Errors;

        // Call: delegate fully-prepared message to the unified notification hub (module boundary: Service → Hub)
        return await notificationHub.SendAsync(message, ct);
    }
}
