using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Application.BuildingBlocks.Ports.Notifications.Store;

/// <summary>Defines the priority-level registry mapping each notification use case to its delivery priority.</summary>
public static partial class NotificationStore
{
    // Assign: Map each use case to its configured priority level
    public static readonly Dictionary<NotificationPriorityLevel, NotificationDefinition<NotificationPriorityLevel>> PriorityLevels = new()
    {
        [NotificationPriorityLevel.Critical] = new NotificationDefinition<NotificationPriorityLevel>
        {
            Value = NotificationPriorityLevel.Critical,
            Name = nameof(NotificationPriorityLevel.Critical),
            Presentation = "Critical",
            Description = "Immediate delivery required for security and authentication purposes.",
            Example = "Suspicious login detected on your account. Please verify your identity immediately."
        },
        [NotificationPriorityLevel.High] = new NotificationDefinition<NotificationPriorityLevel>
        {
            Value = NotificationPriorityLevel.High,
            Name = nameof(NotificationPriorityLevel.High),
            Presentation = "High",
            Description = "Urgent notifications such as failed payments or time-sensitive actions.",
            Example = "Your payment for order #ORD123456 failed. Please update your payment method."
        },
        [NotificationPriorityLevel.Normal] = new NotificationDefinition<NotificationPriorityLevel>
        {
            Value = NotificationPriorityLevel.Normal,
            Name = nameof(NotificationPriorityLevel.Normal),
            Presentation = "Normal",
            Description = "Standard priority notifications such as order updates or account changes.",
            Example = "Your order #ORD123456 has been shipped."
        },
        [NotificationPriorityLevel.Low] = new NotificationDefinition<NotificationPriorityLevel>
        {
            Value = NotificationPriorityLevel.Low,
            Name = nameof(NotificationPriorityLevel.Low),
            Presentation = "Low",
            Description = "Non-urgent notifications such as promotions, newsletters, or general updates.",
            Example = "Summer sale starts next week! Get ready for up to 50% off."
        }
    };

}