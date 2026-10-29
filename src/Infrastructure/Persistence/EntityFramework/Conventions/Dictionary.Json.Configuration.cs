using Core.Common.Monad.Metadata;

using Infrastructure.Persistence.EntityFramework.Comparers;
using Infrastructure.Persistence.EntityFramework.Converters;
using Infrastructure.Persistence.EntityFramework.Providers;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityFramework.Conventions;

/// <summary>
/// Convention that configures dictionary and metadata properties to store as JSON.
/// </summary>
/// <remarks>
/// Supports DefaultMetadata and Dictionary[string,object] types.
/// Uses JSONB for PostgreSQL and NVARCHAR(MAX) for SQL Server.
/// </remarks>
public static class DictionaryAsJsonConvention
{
    /// <summary>Shared comparer for Dictionary[string,object].</summary>
    public static ValueComparer<Dictionary<string, object>> DictionaryComparer { get; } = CreateDictionaryComparer();

    /// <summary>
    /// Applies JSON conversion to dictionary and metadata properties in the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="providerKind">The database provider kind.</param>
    public static void Apply(ModelBuilder modelBuilder, PersistenceProviderKind providerKind)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Skip: owned value objects are configured through their owner's nested builder.
            if (entityType.IsOwned())
                continue;

            foreach (var property in entityType.GetProperties())
            {
                // Skip if already configured with a conversion
                if (property.GetValueConverter() != null)
                    continue;

                // Skip if primary key or foreign key
                if (property.IsPrimaryKey() || property.IsForeignKey())
                    continue;

                var propertyBuilder = modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name);

                // Determine the underlying type for nullable handling
                Type? underlyingType = Nullable.GetUnderlyingType(property.ClrType);

                // Handle DefaultMetadata and DefaultMetadata?
                if (property.ClrType == typeof(DefaultMetadata) || underlyingType == typeof(DefaultMetadata))
                {
                    if (underlyingType == typeof(DefaultMetadata))
                    {
                        propertyBuilder.HasConversion(NullableMetadataValueConverter.Instance);
                    }
                    else
                    {
                        propertyBuilder.HasConversion(MetadataValueConverter.Instance);
                    }
                    SetJsonColumnType(propertyBuilder, providerKind);
                    propertyBuilder.Metadata.SetValueComparer(MetadataValueComparer.Instance);
                }
                // Handle Dictionary{string, object} and Dictionary{string, object}?
                else if (IsStringObjectDictionary(property.ClrType) || IsStringObjectDictionary(underlyingType))
                {
                    if (underlyingType != null)
                    {
                        propertyBuilder.HasConversion(NullableDictionaryJsonValueConverter.Instance);
                    }
                    else
                    {
                        propertyBuilder.HasConversion(DictionaryJsonValueConverter.Instance);
                    }
                    SetJsonColumnType(propertyBuilder, providerKind);
                    propertyBuilder.Metadata.SetValueComparer(DictionaryComparer);
                }
            }
        }
    }

    static bool IsStringObjectDictionary(Type? type)
    {
        return type != null && type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
               type.GetGenericArguments()[0] == typeof(string) &&
               type.GetGenericArguments()[1] == typeof(object);
    }

    private static void SetJsonColumnType(PropertyBuilder propertyBuilder, PersistenceProviderKind providerKind)
    {
        switch (providerKind)
        {
            case PersistenceProviderKind.PostgreSql:
                propertyBuilder.HasColumnType("jsonb");
                break;

            case PersistenceProviderKind.SqlServer:
                propertyBuilder.HasColumnType("nvarchar(max)");
                break;

            case PersistenceProviderKind.Sqlite:
                propertyBuilder.HasColumnType("TEXT");
                break;

            case PersistenceProviderKind.InMemory:
                // InMemory provider ignores column types
                break;
        }
    }


    private static ValueComparer<Dictionary<string, object>> CreateDictionaryComparer()
    {
        return new ValueComparer<Dictionary<string, object>>(
            (left, right) => EqualsDictionary(left, right),
            value => HashDictionary(value),
            value => new Dictionary<string, object>(value, StringComparer.OrdinalIgnoreCase));
    }

    private static bool EqualsDictionary(Dictionary<string, object>? left, Dictionary<string, object>? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null || left.Count != right.Count)
            return false;

        foreach (var pair in left)
        {
            if (!TryGetValue(right, pair.Key, out object? value) || !Equals(pair.Value, value))
                return false;
        }

        return true;
    }

    private static bool TryGetValue(Dictionary<string, object> dictionary, string key, out object? value)
    {
        // Keys: the snapshot uses OrdinalIgnoreCase, so equality must ignore case too.
        if (dictionary.TryGetValue(key, out value))
            return true;

        foreach (var pair in dictionary)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(pair.Key, key))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static int HashDictionary(Dictionary<string, object> value)
    {
        // Hash: order-independent so equal dictionaries always hash equally.
        var hash = 0;
        foreach (var pair in value)
        {
            hash ^= HashCode.Combine(
                pair.Key.GetHashCode(StringComparison.OrdinalIgnoreCase),
                pair.Value?.GetHashCode() ?? 0);
        }

        return hash;
    }

    // ─── Fluent extension methods ─────────────────────────────────────────

    /// <summary>
    /// Configures a <see cref="DefaultMetadata"/> property to store as JSONB with proper
    /// value conversion and change tracking.
    /// </summary>
    /// <param name="propertyBuilder">The property builder for the metadata property.</param>
    /// <returns>The property builder for further chaining.</returns>
    /// <remarks>
    /// Applies <see cref="MetadataValueConverter"/>, sets column type to <c>jsonb</c>,
    /// and configures <see cref="MetadataValueComparer"/> for change tracking.
    /// </remarks>
    public static PropertyBuilder<DefaultMetadata> HasMetadataConversion(
        this PropertyBuilder<DefaultMetadata> propertyBuilder)
    {
        propertyBuilder.HasConversion(MetadataValueConverter.Instance);
        propertyBuilder.HasColumnType("jsonb");
        propertyBuilder.Metadata.SetValueComparer(MetadataValueComparer.Instance);
        return propertyBuilder;
    }

    /// <summary>
    /// Configures a <see cref="Dictionary{TKey, TValue}"/> property to store as JSONB with
    /// proper value conversion and change tracking.
    /// </summary>
    /// <param name="propertyBuilder">The property builder for the dictionary property.</param>
    /// <returns>The property builder for further chaining.</returns>
    /// <remarks>
    /// Applies <see cref="DictionaryJsonValueConverter"/>, sets column type to <c>jsonb</c>,
    /// and configures a structural value comparer for change tracking.
    /// </remarks>
    public static PropertyBuilder<Dictionary<string, object>> HasJsonbConversion(
        this PropertyBuilder<Dictionary<string, object>> propertyBuilder)
    {
        propertyBuilder.HasConversion(DictionaryJsonValueConverter.Instance);
        propertyBuilder.HasColumnType("jsonb");
        propertyBuilder.Metadata.SetValueComparer(DictionaryComparer);
        return propertyBuilder;
    }

    /// <summary>
    /// Configures a <see cref="ICollection{Guid}"/> property to store as a JSON array
    /// in a JSONB column with proper value conversion and change tracking.
    /// </summary>
    /// <param name="propertyBuilder">The property builder for the Guid collection property.</param>
    /// <returns>The property builder for further chaining.</returns>
    /// <remarks>
    /// Applies <see cref="GuidListJsonValueConverter"/>, sets column type to <c>jsonb</c>,
    /// and configures a structural value comparer for change tracking.
    /// </remarks>
    public static PropertyBuilder<ICollection<Guid>> HasGuidListConversion(
        this PropertyBuilder<ICollection<Guid>> propertyBuilder)
    {
        propertyBuilder.HasConversion(GuidListJsonValueConverter.Instance);
        propertyBuilder.HasColumnType("jsonb");
        propertyBuilder.Metadata.SetValueComparer(CreateGuidListComparer());
        return propertyBuilder;
    }

    static ValueComparer<ICollection<Guid>> CreateGuidListComparer()
    {
        return new ValueComparer<ICollection<Guid>>(
            (left, right) => ReferenceEquals(left, right) ||
                (left != null && right != null && left.SequenceEqual(right)),
            value => value.Aggregate(0, (hash, id) => HashCode.Combine(hash, id.GetHashCode())),
            value => value.ToList());
    }

}
