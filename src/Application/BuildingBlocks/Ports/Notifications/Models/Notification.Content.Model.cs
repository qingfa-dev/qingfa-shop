namespace Application.BuildingBlocks.Ports.Notifications.Models;

/// <summary>
/// Represents the final rendered content of a notification, ready for delivery.
/// </summary>
public sealed record NotificationContent
{
    /// <summary>Gets the notification subject or title.</summary>
    public string Subject { get; init; }

    /// <summary>Gets the plain-text body of the message.</summary>
    public string Body { get; init; }

    /// <summary>Gets the optional HTML-formatted body of the message.</summary>
    public string? HtmlBody { get; init; }

    private NotificationContent(string subject, string body, string? htmlBody = null)
    {
        Subject = subject;
        Body = body;
        HtmlBody = htmlBody;
    }

    /// <summary>
    /// Creates a new instance of <see cref="NotificationContent"/>.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <param name="body">The plain text body.</param>
    /// <param name="htmlBody">The optional HTML body.</param>
    /// <returns>A new <see cref="NotificationContent"/>.</returns>
    public static NotificationContent Create(string subject, string body, string? htmlBody = null)
        => new(subject, body, htmlBody);
}
