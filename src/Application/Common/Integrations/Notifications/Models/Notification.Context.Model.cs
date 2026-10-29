using Application.Common.Integrations.Notifications.Templates;

namespace Application.Common.Integrations.Notifications.Models;

/// <summary>
/// A refined container for notification template parameters stored as an optimized list of key-value pairs.
/// </summary>
public sealed record NotificationContext
{
    /// <summary>Gets the collection of parameters available in this context.</summary>
    public IReadOnlyList<NotificationParameter> Parameters { get; init; }

    internal NotificationContext(IEnumerable<NotificationParameter> parameters)
    {
        Parameters = parameters.ToList().AsReadOnly();
    }

    private NotificationContext()
    {
        Parameters = new List<NotificationParameter>().AsReadOnly();
    }

    /// <summary>
    /// Retrieves the string value for a specific parameter type if it exists in the context.
    /// </summary>
    /// <param name="parameter">The parameter type to look up.</param>
    /// <returns>The value if found; otherwise, null.</returns>
    public string? GetValue(NotificationParameterType parameter)
    {
        // Merge: Last-one-wins for duplicate parameter keys
        return Parameters.LastOrDefault(p => p.Key == parameter)?.Value;
    }

    /// <summary>Gets an empty <see cref="NotificationContext"/>.</summary>
    public static NotificationContext Empty => new();

    /// <summary>
    /// Creates a new <see cref="NotificationContext"/> from a set of key-value tuples.
    /// </summary>
    /// <param name="items">The items to include.</param>
    /// <returns>A new <see cref="NotificationContext"/>.</returns>
    public static NotificationContext Create(params (NotificationParameterType Key, string? Value)[] items)
    {
        // Ensure: Deduplicate parameters by key before creating context
        var list = items
            .GroupBy(i => i.Key)
            .Select(g => NotificationParameter.Create(g.Key, g.Last().Value))
            .ToList();

        return new NotificationContext(list);
    }

    /// <summary>
    /// Applies a parameter to an existing context, returning a new context instance.
    /// </summary>
    /// <param name="context">The source context.</param>
    /// <param name="key">The key to add or update.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>A new <see cref="NotificationContext"/> with the updated parameter.</returns>
    public static NotificationContext ApplyParameter(NotificationContext context, NotificationParameterType key, string? value)
    {
        var newList = new List<NotificationParameter>(context.Parameters);

        // Ensure: Remove existing key before upserting to maintain flat parameter dictionary
        newList.RemoveAll(p => p.Key == key);
        newList.Add(NotificationParameter.Create(key, value));

        return new NotificationContext(newList);
    }
}
