using Core.Todos.Items;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Conventions;
using Infrastructure.Persistence.EntityFramework.Converters;
using Infrastructure.Persistence.EntityFramework.Providers;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Conventions;

/// <summary>
/// Tests that model conventions run after entity configurations and that each
/// convention respects explicit configuration and the active provider.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class ConventionTest
{
    // ─── Ordering ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Conventions should apply to entities registered before base OnModelCreating")]
    public void Model_ShouldApplyConventionsToConfiguredEntities()
    {
        // Arrange
        using var context = new ConventionDbContext(SqliteOptions<ConventionDbContext>());
        var entity = context.Model.FindEntityType(typeof(ConventionEntity))!;

        // Act
        var amount = entity.FindProperty(nameof(ConventionEntity.Amount))!;
        var adjustment = entity.FindProperty(nameof(ConventionEntity.Adjustment))!;
        var createdAt = entity.FindProperty(nameof(ConventionEntity.CreatedAtUtc))!;
        var state = entity.FindProperty(nameof(ConventionEntity.State))!;
        var previousState = entity.FindProperty(nameof(ConventionEntity.PreviousState))!;
        var metadata = entity.FindProperty(nameof(ConventionEntity.Metadata))!;
        var payload = entity.FindProperty(nameof(ConventionEntity.Payload))!;
        var optionalPayload = entity.FindProperty(nameof(ConventionEntity.OptionalPayload))!;

        // Assert - DateTimeOffset convention
        createdAt.GetColumnType().Should().Be("TEXT");
        entity.FindProperty(nameof(ConventionEntity.ArchivedAtUtc))!
            .GetColumnType().Should().Be("TEXT");

        // Assert - decimal convention
        amount.GetColumnType().Should().Be("REAL");
        adjustment.GetColumnType().Should().Be("REAL");

        // Assert - enum convention
        state.GetValueConverter().Should().NotBeNull();
        state.GetValueConverter()!.ProviderClrType.Should().Be<int>();
        previousState.GetValueConverter().Should().NotBeNull();
        previousState.GetValueConverter()!.ProviderClrType.Should().Be<int?>();

        // Assert - dictionary convention
        metadata.GetValueConverter().Should().NotBeNull();
        metadata.GetValueComparer().Should().NotBeNull();
        metadata.GetColumnType().Should().Be("TEXT");
        payload.GetValueConverter().Should().NotBeNull();
        payload.GetValueComparer().Should().NotBeNull();
        payload.GetColumnType().Should().Be("TEXT");
        optionalPayload.GetValueConverter().Should().NotBeNull();
        optionalPayload.GetValueComparer().Should().NotBeNull();
        optionalPayload.GetColumnType().Should().Be("TEXT");
    }

    [Fact(DisplayName = "Conventions should not override an explicitly configured property")]
    public void Model_WhenPropertyIsExplicitlyConfigured_ShouldKeepTheExplicitConfiguration()
    {
        // Arrange
        using var context = new ConventionDbContext(SqliteOptions<ConventionDbContext>());
        var entity = context.Model.FindEntityType(typeof(ConventionEntity))!;

        // Act
        var total = entity.FindProperty(nameof(ConventionEntity.Total))!;
        var releasedAt = entity.FindProperty(nameof(ConventionEntity.ReleasedAtUtc))!;

        // Assert
        total.GetPrecision().Should().Be(12);
        total.GetScale().Should().Be(2);
        releasedAt.GetColumnType().Should().Be("datetime2");
    }

    [Fact(DisplayName = "Conventions should apply to the Todo entities configured from the Infrastructure assembly")]
    public void Model_ShouldApplyConventionsToAssemblyConfiguredEntities()
    {
        // Arrange
        using var context = new ConventionDbContext(SqliteOptions<ConventionDbContext>());

        // Act
        var todo = context.Model.FindEntityType(typeof(Todo))!;
        var dueAt = todo.FindProperty(nameof(Todo.DueAtUtc))!;
        var status = todo.FindProperty(nameof(Todo.Status))!;

        // Assert
        dueAt.GetColumnType().Should().Be("TEXT");
        status.GetValueConverter().Should().BeOfType<EnumValueConverter<TodoStatus>>();
        todo.FindProperty(nameof(Todo.Metadata))!
            .GetValueConverter().Should().BeOfType<MetadataValueConverter>();
    }

    // ─── Assembly resolution ──────────────────────────────────────────────

    [Fact(DisplayName = "A derived context in another assembly should still apply Infrastructure entity configurations")]
    public void Model_WhenContextLivesInAnotherAssembly_ShouldLoadInfrastructureConfigurations()
    {
        // Arrange
        using var context = new InheritedDbContext(SqliteOptions<InheritedDbContext>());

        // Act
        var entity = context.Model.FindEntityType(typeof(Todo));

        // Assert
        entity.Should().NotBeNull();
        entity!.GetTableName().Should().Be("todos");
        entity.GetSchema().Should().Be("todo");
    }

    // ─── Provider mapping ─────────────────────────────────────────────────

    [Fact(DisplayName = "SQLite should map timestamps to TEXT instead of forcing timestamptz")]
    public void Model_WhenProviderIsSqlite_ShouldMapTimestampsToText()
    {
        // Arrange
        using var context = new ApplicationDbContext(SqliteOptions<ApplicationDbContext>());

        // Act
        var dueAt = context.Model.FindEntityType(typeof(Todo))!
            .FindProperty(nameof(Todo.DueAtUtc))!;

        // Assert
        dueAt.GetColumnType().Should().Be("TEXT");
    }

    [Fact(DisplayName = "PostgreSQL should map timestamps to timestamptz")]
    public void Model_WhenProviderIsPostgreSql_ShouldMapTimestampsToTimestampTz()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=qingfa_shop;Username=postgres;Password=postgres")
            .Options;
        using var context = new ApplicationDbContext(options);

        // Act
        var dueAt = context.Model.FindEntityType(typeof(Todo))!
            .FindProperty(nameof(Todo.DueAtUtc))!;

        // Assert
        dueAt.GetColumnType().Should().Be("timestamptz");
    }

    // ─── Selective application ────────────────────────────────────────────

    [Fact(DisplayName = "ApplyConventions should honour the per-convention switches")]
    public void ApplyConventions_WhenDecimalIsDisabled_ShouldSkipTheDecimalConvention()
    {
        // Arrange
        using var context = new SelectiveConventionDbContext(SqliteOptions<SelectiveConventionDbContext>());
        var entity = context.Model.FindEntityType(typeof(ConventionEntity))!;

        // Act
        var amount = entity.FindProperty(nameof(ConventionEntity.Amount))!;
        var state = entity.FindProperty(nameof(ConventionEntity.State))!;

        // Assert
        amount.GetColumnType().Should().NotBe("REAL");
        state.GetValueConverter().Should().NotBeNull();
    }

    [Fact(DisplayName = "ApplyDefaultConventions should reject a null model builder")]
    public void ApplyDefaultConventions_WhenNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        ModelBuilder? modelBuilder = null;

        // Act
        Action act = () => modelBuilder!.ApplyDefaultConventions(PersistenceProviderKind.Sqlite);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private static DbContextOptions<TContext> SqliteOptions<TContext>()
        where TContext : Microsoft.EntityFrameworkCore.DbContext
        => new DbContextOptionsBuilder<TContext>()
            .UseSqlite($"Data Source=Conventions_{Guid.NewGuid():N}.db")
            .Options;
}

/// <summary>Enum used to verify the enum-as-int convention.</summary>
public enum ConventionState
{
    Draft,
    Published
}

/// <summary>Entity without explicit type configuration so conventions own the mappings.</summary>
public sealed class ConventionEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ArchivedAtUtc { get; set; }
    public ConventionState State { get; set; }
    public ConventionState? PreviousState { get; set; }
    public decimal Amount { get; set; }
    public decimal? Adjustment { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset ReleasedAtUtc { get; set; }
    public DefaultMetadata Metadata { get; set; } = new();
    public Dictionary<string, object> Payload { get; set; } = new();
    public Dictionary<string, object>? OptionalPayload { get; set; }
}

/// <summary>Configuration that intentionally leaves type mapping to the conventions.</summary>
public sealed class ConventionEntityConfiguration : IEntityTypeConfiguration<ConventionEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConventionEntity> builder)
    {
        builder.ToTable("convention_entities");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(entity => entity.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(entity => entity.ArchivedAtUtc).HasColumnName("archived_at_utc");
        builder.Property(entity => entity.State).HasColumnName("state");
        builder.Property(entity => entity.PreviousState).HasColumnName("previous_state");
        builder.Property(entity => entity.Amount).HasColumnName("amount");
        builder.Property(entity => entity.Adjustment).HasColumnName("adjustment");
        builder.Property(entity => entity.Metadata).HasColumnName("metadata");
        builder.Property(entity => entity.Payload).HasColumnName("payload");
        builder.Property(entity => entity.OptionalPayload).HasColumnName("optional_payload");

        // Explicit: the conventions must leave these alone.
        builder.Property(entity => entity.Total)
            .HasColumnName("total")
            .HasPrecision(12, 2);
        builder.Property(entity => entity.ReleasedAtUtc)
            .HasColumnName("released_at_utc")
            .HasColumnType("datetime2");
    }
}

/// <summary>Context that registers a configuration before the base conventions run.</summary>
public sealed class ConventionDbContext(DbContextOptions<ConventionDbContext> options) : EfCoreDbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ConventionEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

/// <summary>Context defined outside the Infrastructure assembly to verify assembly resolution.</summary>
public sealed class InheritedDbContext(DbContextOptions<InheritedDbContext> options) : EfCoreDbContext(options);

/// <summary>Context that applies conventions selectively without the defaults.</summary>
public sealed class SelectiveConventionDbContext(DbContextOptions<SelectiveConventionDbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ConventionEntityConfiguration());
        modelBuilder.ApplyConventions(
            PersistenceProviderKind.Sqlite,
            applyDateTimeOffsetConvention: true,
            applyEnumConvention: true,
            applyDictionaryConvention: true,
            applyDecimalConvention: false);
    }
}
