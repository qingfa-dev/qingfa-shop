using Core.BuildingBlocks.Modeling.Metadata;


namespace Application.BuildingBlocks.Ports.Notifications.Templates;

/// <summary>
/// A base record for defining notification metadata associated with an enumeration type.
/// </summary>
/// <typeparam name="TEnumerate">The enumeration type (e.g., UseCase, SendMethod).</typeparam>
public record NotificationDefinition<TEnumerate> : IHasMetadata
    where TEnumerate : Enum
{
    /// <summary>Gets arbitrary template metadata.</summary>
    public DefaultMetadata Metadata { get; init; } = [];

    /// <summary>Gets or sets the enumerated value described by this definition.</summary>
    public TEnumerate Value { get; init; } = default!;

    /// <summary>Gets or sets the machine-readable name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets or sets the human-readable description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets or sets an optional example value.</summary>
    public object? Example { get; init; }

    /// <summary>Gets or sets the display name or label for presentation.</summary>
    public string Presentation { get; init; } = null!;
}

/// <summary>
/// Represents a structured template for a specific notification use case.
/// </summary>
public sealed record NotificationTemplate : NotificationDefinition<NotificationUseCase>
{
    /// <summary>
    /// Gets or sets the preferred delivery method for this template.
    /// </summary>
    public NotificationChannel SendMethodType { get; set; } = NotificationChannel.Email;

    /// <summary>
    /// Gets or sets the format of the template content.
    /// </summary>
    public NotificationFormat TemplateFormatType { get; set; } = NotificationFormat.Default;

    /// <summary>
    /// Gets or sets the urgency/priority level for this notification.
    /// </summary>
    public NotificationPriorityLevel Priority { get; set; } = NotificationPriorityLevel.Normal;

    /// <summary>
    /// Gets or sets the list of parameters required to fill this template.
    /// </summary>
    public List<NotificationParameterType> ParamValues { get; set; } = [];

    /// <summary>
    /// Gets or sets the plain text content of the template.
    /// </summary>
    public string? TemplateContent { get; set; }

    /// <summary>
    /// Gets or sets the HTML content of the template.
    /// </summary>
    public string? HtmlTemplateContent { get; set; }
}
