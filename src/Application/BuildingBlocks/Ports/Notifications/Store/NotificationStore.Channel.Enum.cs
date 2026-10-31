using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Application.BuildingBlocks.Ports.Notifications.Store;

/// <summary>Defines the channel-to-provider mapping registry. Maps each notification use case to its allowed delivery channels.</summary>
public static partial class NotificationStore
{
    // Assign: Map each use case to its allowed delivery channels
    public static readonly Dictionary<NotificationChannel, NotificationDefinition<NotificationChannel>> SendMethods = new()
    {
        [NotificationChannel.None] = new NotificationDefinition<NotificationChannel>
        {
            Value = NotificationChannel.None,
            Name = nameof(NotificationChannel.None),
            Presentation = "None",
            Description = "No notification will be sent. Used for draft or inactive templates.",
            Example = string.Empty
        },
        [NotificationChannel.Email] = new NotificationDefinition<NotificationChannel>
        {
            Value = NotificationChannel.Email,
            Name = nameof(NotificationChannel.Email),
            Presentation = "Email",
            Description = "Sends notifications via email. Ideal for order confirmations, promotions, and account updates.",
            Example = "Subject: Your Order #ORD123456 Has Shipped!"
        },
        [NotificationChannel.SMS] = new NotificationDefinition<NotificationChannel>
        {
            Value = NotificationChannel.SMS,
            Name = nameof(NotificationChannel.SMS),
            Presentation = "SMS",
            Description = "Sends notifications via text message. Best for short, time-sensitive alerts like delivery updates or OTP codes.",
            Example = "Your OTP code is 123456."
        },
        [NotificationChannel.PushNotification] = new NotificationDefinition<NotificationChannel>
        {
            Value = NotificationChannel.PushNotification,
            Name = nameof(NotificationChannel.PushNotification),
            Presentation = "Push Notification",
            Description = "Sends push notifications through a mobile app. Commonly used for promotions, flash sales, and real-time order status updates.",
            Example = "Flash Sale! Get 20% off for the next 2 hours."
        },
        [NotificationChannel.WhatsApp] = new NotificationDefinition<NotificationChannel>
        {
            Value = NotificationChannel.WhatsApp,
            Name = nameof(NotificationChannel.WhatsApp),
            Presentation = "WhatsApp",
            Description = "Sends notifications via WhatsApp. Popular in some regions for direct customer engagement and order support.",
            Example = "Hi Jane, your order #ORD123456 has been delivered. Thank you for shopping with us!"
        }
    };
}