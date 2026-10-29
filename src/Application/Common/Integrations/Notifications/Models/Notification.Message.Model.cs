using Application.Common.Integrations.Notifications.Services;
using Application.Common.Integrations.Notifications.Templates;

namespace Application.Common.Integrations.Notifications.Models;

/// <summary>
/// A high-level request object representing a notification to be sent.
/// This is the primary input for the <see cref="INotificationService"/>.
/// </summary>
public sealed record NotificationMessage
{
    /// <summary>Gets the specific scenario/scenario template to use.</summary>
    public NotificationUseCase UseCase { get; init; }

    /// <summary>Gets the target recipient details.</summary>
    public NotificationRecipient Recipient { get; init; }

    /// <summary>Gets the data context used to fill template placeholders.</summary>
    public NotificationContext Context { get; init; }

    /// <summary>Gets an optional list of file attachments (typically for Email).</summary>
    public List<NotificationAttachment>? Attachments { get; init; }

    /// <summary>Gets the delivery metadata (Priority, Language, etc.).</summary>
    public Dictionary<string, object?> Metadata { get; init; }

    public NotificationChannel Channel { get; set; }

    internal NotificationMessage(
        NotificationUseCase useCase,
        NotificationRecipient recipient,
        NotificationChannel channel,
        NotificationContext context,
        List<NotificationAttachment>? attachments = null,
        params (string key, object? value)[] metadata)
    {
        UseCase = useCase;
        Recipient = recipient;
        Channel = channel;
        Context = context;
        Attachments = attachments;
        Metadata = metadata.ToDictionary();
    }

    /// <summary>
    /// Creates a new <see cref="NotificationMessage"/> instance.
    /// </summary>
    /// <param name="useCase">The use case.</param>
    /// <param name="recipient">The recipient.</param>
    /// <param name="channel">The delivery channel.</param>
    /// <param name="context">The data context.</param>
    /// <param name="attachments">Optional attachments.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A new <see cref="NotificationMessage"/>.</returns>
    public static NotificationMessage Create(
        NotificationUseCase useCase,
        NotificationRecipient recipient,
        NotificationChannel channel,
        NotificationContext context,
        List<NotificationAttachment>? attachments = null,
        params (string key, object? value)[] metadata)
    {
        return new NotificationMessage(useCase, recipient, channel, context, attachments, metadata);
    }
}
