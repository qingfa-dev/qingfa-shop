using Application.Common.Integrations.Notifications.Templates;

namespace Application.Common.Integrations.Notifications.Store;

/// <summary>Defines the notification template format registry. Maps each format type to its presentation metadata.</summary>
public static partial class NotificationStore
{
    // Create: Register format presentation metadata for each supported format type
    public static readonly Dictionary<NotificationFormat, NotificationDefinition<NotificationFormat>> Formats = new()
    {
        [NotificationFormat.Default] = new NotificationDefinition<NotificationFormat>
        {
            Name = nameof(NotificationFormat.Default),
            Value = NotificationFormat.Default,
            Presentation = "Default (Plain Text)",
            Description = "A plain text template without advanced styling. Suitable for SMS, WhatsApp, and basic email notifications where formatting is minimal.",
            Example = "Your order #ORD123456 has been shipped."
        },
        [NotificationFormat.Html] = new NotificationDefinition<NotificationFormat>
        {
            Name = nameof(NotificationFormat.Html),
            Value = NotificationFormat.Html,
            Presentation = "HTML",
            Description = "A rich HTML template with styling, images, and layout control. Commonly used for marketing emails and branded notifications.",
            Example = "<html><body><h1>Order Shipped</h1><p>Your order #ORD123456 is on the way!</p></body></html>"
        }
    };

}