using Application.Common.Persistence;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Providers;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Conventions;

/// <summary>
/// Extension methods for applying EF Core conventions to <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderConventionExtensions
{
    /// <summary>
    /// Applies the repository's schema, explicit entity configurations, and provider conventions.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <param name="providerName">The EF Core provider name, when a context is available.</param>
    public static void ApplyPersistenceConventions(
        this ModelBuilder modelBuilder,
        string? providerName)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(PersistenceConstant.Schema.Default);

        PersistenceProviderKind providerKind = PersistenceProviderDetector.Detect(providerName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EfCoreDbContext).Assembly);
        modelBuilder.ApplyDefaultConventions(providerKind);
    }

    /// <summary>
    /// Applies all default conventions to the model builder.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <param name="providerKind">The database provider kind for provider-specific settings.</param>
    /// <remarks>
    /// This applies the following conventions in order:
    /// <list type="number">
    ///   <item><description>DateTimeOffset properties get provider-specific column types (timestamptz, datetime2, etc.)</description></item>
    ///   <item><description>Enum properties are stored as their underlying integer type</description></item>
    ///   <item><description>Dictionary and DefaultMetadata properties are stored as JSON</description></item>
    ///   <item><description>Decimal properties get precision 18 and scale 6</description></item>
    ///   <item><description>Versionable aggregates use their version property as an EF Core concurrency token</description></item>
    /// </list>
    /// Conventions skip properties that are already explicitly configured.
    /// </remarks>
    public static void ApplyDefaultConventions(this ModelBuilder modelBuilder, PersistenceProviderKind providerKind)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Apply conventions in order - each convention skips already-configured properties
        DateTimeOffsetConvention.Apply(modelBuilder, providerKind);
        EnumAsIntConvention.Apply(modelBuilder, providerKind);
        DictionaryAsJsonConvention.Apply(modelBuilder, providerKind);
        DecimalConvention.Apply(modelBuilder, providerKind);
        VersionableConfiguration.Apply(modelBuilder);
    }

    /// <summary>
    /// Applies specific conventions to the model builder.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <param name="providerKind">The database provider kind for provider-specific settings.</param>
    /// <param name="applyDateTimeOffsetConvention">Whether to apply DateTimeOffset convention.</param>
    /// <param name="applyEnumConvention">Whether to apply enum convention.</param>
    /// <param name="applyDictionaryConvention">Whether to apply dictionary/JSON convention.</param>
    /// <param name="applyDecimalConvention">Whether to apply decimal convention.</param>
    public static void ApplyConventions(
        this ModelBuilder modelBuilder,
        PersistenceProviderKind providerKind,
        bool applyDateTimeOffsetConvention = true,
        bool applyEnumConvention = true,
        bool applyDictionaryConvention = true,
        bool applyDecimalConvention = true)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        if (applyDateTimeOffsetConvention)
            DateTimeOffsetConvention.Apply(modelBuilder, providerKind);

        if (applyEnumConvention)
            EnumAsIntConvention.Apply(modelBuilder, providerKind);

        if (applyDictionaryConvention)
            DictionaryAsJsonConvention.Apply(modelBuilder, providerKind);

        if (applyDecimalConvention)
            DecimalConvention.Apply(modelBuilder, providerKind);

        // Enforce: the concurrency token is a persistence invariant, not an optional convention.
        VersionableConfiguration.Apply(modelBuilder);
    }
}