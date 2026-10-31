namespace Application.BuildingBlocks.Ports.Notifications.Models;

/// <summary>
/// Represents the target recipient of a notification.
/// </summary>
public sealed record NotificationRecipient
{
    /// <summary>Gets the unique identifier for delivery (e.g., Email address or Phone number).</summary>
    public string Identifier { get; init; }

    /// <summary>Gets the optional display name of the recipient.</summary>
    public string? Name { get; init; }

    private NotificationRecipient(string identifier, string? name = null)
    {
        Identifier = identifier;
        Name = name;
    }

    /// <summary>
    /// Creates a new instance of <see cref="NotificationRecipient"/>.
    /// </summary>
    /// <param name="identifier">The delivery identifier.</param>
    /// <param name="name">The optional name.</param>
    /// <returns>A new <see cref="NotificationRecipient"/>.</returns>
    public static NotificationRecipient Create(string identifier, string? name = null) => new(identifier, name);
}
