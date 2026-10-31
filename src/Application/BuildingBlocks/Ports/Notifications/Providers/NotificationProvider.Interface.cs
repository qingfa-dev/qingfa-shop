using Application.BuildingBlocks.Ports.Notifications.Models;

using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Application.BuildingBlocks.Ports.Notifications.Providers;

/// <summary>
/// Defines the internal contract for an SMS delivery provider (e.g., Sinch).
/// </summary>
public interface INotificationProvider
{
    /// <summary>
    /// Gets the unique name of the provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the execution priority (lower is higher priority).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether the provider is currently enabled in configuration.
    /// </summary>
    bool IsEnabled { get; }
    
    public NotificationChannel Channel { get; }

    /// <summary>
    /// Attempts to send an SMS via this specific provider.
    /// </summary>
    Task<Result> SendAsync(
        NotificationMessage message,
        CancellationToken ct = default);
}
