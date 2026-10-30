using System.Text;

namespace Application.Common.EventSourcing;

/// <summary>Builds stable stream identifiers from aggregate types.</summary>
public static class EventStreamName
{
    /// <summary>Builds <c>{kebab-aggregate}:{id}</c>.</summary>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="id">The aggregate id.</param>
    /// <returns>The stable stream identifier.</returns>
    public static string For<TAggregate>(Guid id)
    {
        string name = typeof(TAggregate).Name;
        if (name.EndsWith("Aggregate", StringComparison.Ordinal))
            name = name[..^"Aggregate".Length];

        var builder = new StringBuilder(name.Length + 8);
        for (int index = 0; index < name.Length; index++)
        {
            char current = name[index];
            if (index > 0 && char.IsUpper(current))
                builder.Append('-');

            builder.Append(char.ToLowerInvariant(current));
        }

        return $"{builder}:{id:D}";
    }
}
