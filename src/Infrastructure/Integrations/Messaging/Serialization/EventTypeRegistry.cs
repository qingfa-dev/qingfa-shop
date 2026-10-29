using System.Reflection;

using Application.Common.Messaging.Serialization;

using Core.Common.Domain.Events;

namespace Infrastructure.Integrations.Messaging.Serialization;

/// <summary>Resolves stable event names and versions to CLR types.</summary>
/// <remarks>
/// Domain events are discovered by scanning the supplied assemblies and are registered under
/// their CLR type name at version <c>1</c>. Integration events cannot be inferred from payloads,
/// so they are registered explicitly through <see cref="Register{T}"/>.
/// </remarks>
public sealed class EventTypeRegistry : IEventTypeRegistry
{
    private const int DefaultDomainEventVersion = 1;

    private readonly Dictionary<string, Type> _types = new(StringComparer.Ordinal);

    /// <summary>Creates a registry and discovers domain events in the supplied assemblies.</summary>
    /// <param name="assemblies">Assemblies scanned for <see cref="IDomainEvent"/> implementations.</param>
    public EventTypeRegistry(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (Assembly assembly in assemblies)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                // Skip: an open generic cannot be materialised from a payload.
                if (type.IsGenericTypeDefinition || type.ContainsGenericParameters)
                    continue;

                if (typeof(IDomainEvent).IsAssignableFrom(type))
                    Register(type, type.Name, DefaultDomainEventVersion);
            }
        }
    }

    /// <summary>Registers <typeparamref name="T"/> under a stable wire identity.</summary>
    /// <typeparam name="T">The event payload type.</typeparam>
    /// <param name="eventName">The stable wire name.</param>
    /// <param name="eventVersion">The payload version; must be positive.</param>
    public void Register<T>(string eventName, int eventVersion) where T : class
        => Register(typeof(T), eventName, eventVersion);

    /// <summary>Registers an event payload type under a stable wire identity.</summary>
    /// <param name="type">The event payload type.</param>
    /// <param name="eventName">The stable wire name.</param>
    /// <param name="eventVersion">The payload version; must be positive.</param>
    public void Register(Type type, string eventName, int eventVersion)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(eventVersion);

        string key = Key(eventName, eventVersion);

        // Enforce: a stable identity must map to exactly one payload type across the process.
        if (_types.TryGetValue(key, out Type? existing) && existing != type)
        {
            throw new InvalidOperationException(
                $"Event '{eventName}' version {eventVersion} is already registered for " +
                $"'{existing.FullName}' and cannot be remapped to '{type.FullName}'.");
        }

        _types[key] = type;
    }

    /// <inheritdoc />
    public Type Resolve(string eventName, int eventVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        // Fail: an unregistered identity means the consumer cannot safely materialise the payload.
        return _types.TryGetValue(Key(eventName, eventVersion), out Type? type)
            ? type
            : throw new InvalidOperationException(
                $"No event type is registered for '{eventName}' version {eventVersion}.");
    }

    private static string Key(string eventName, int eventVersion)
        => $"{eventName}:{eventVersion}";
}
