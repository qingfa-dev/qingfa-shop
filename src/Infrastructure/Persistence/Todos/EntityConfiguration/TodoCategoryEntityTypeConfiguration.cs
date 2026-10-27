using Core.Todos.Categories;

using Infrastructure.Persistence.EntityFramework.Comparers;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Todos.EntityConfiguration;

/// <summary>
/// Configures the EF Core mapping for the TodoCategory entity.
/// Defines columns, constraints, and indexes for todo category persistence.
/// </summary>
public sealed class TodoCategoryEntityTypeConfiguration : IEntityTypeConfiguration<TodoCategory>
{
    /// <summary>Applies the mapping configuration for the TodoCategory entity.</summary>
    /// <param name="builder">The entity type builder for TodoCategory.</param>
    public void Configure(EntityTypeBuilder<TodoCategory> builder)
    {
        // Table: Use snake_case naming convention consistent with the project.
        builder.ToTable("todo_categories");

        // Key: Primary key is the Guid identifier.
        builder.HasKey(t => t.Id);

        // Identity: Core properties
        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(TodoCategoryConstant.Constraints.Name.MaxLength)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(TodoCategoryConstant.Constraints.Description.MaxLength);

        // Lifecycle: Creation audit fields.
        builder.Property(t => t.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(256);

        builder.Property(t => t.ModifiedAtUtc)
            .HasColumnName("modified_at_utc")
            .HasColumnType("timestamptz");

        builder.Property(t => t.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(256);

        // Soft delete: Deletion audit fields.
        builder.Property(t => t.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(t => t.DeletedAtUtc)
            .HasColumnName("deleted_at_utc")
            .HasColumnType("timestamptz");

        builder.Property(t => t.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(256);

        // Tenancy: Tenant isolation.
        builder.Property(t => t.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        // Metadata: JSON storage for extensible metadata.
        builder.Property(t => t.Metadata)
            .HasColumnName("metadata")
            .HasConversion(
                (Core.Common.Monad.Metadata.DefaultMetadata v) => System.Text.Json.JsonSerializer.Serialize(v),
                (string v) => DeserializeMetadata(v))
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(MetadataValueComparer.Instance);

        // Indexes: Query performance for common filters.
        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => t.CreatedAtUtc);
        builder.HasIndex(t => new { t.TenantId, t.IsDeleted });
    }

    /// <summary>Deserializes a JSON string into a DefaultMetadata instance.</summary>
    private static Core.Common.Monad.Metadata.DefaultMetadata DeserializeMetadata(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new Core.Common.Monad.Metadata.DefaultMetadata();

        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        return dict is null
            ? new Core.Common.Monad.Metadata.DefaultMetadata()
            : new Core.Common.Monad.Metadata.DefaultMetadata(dict);
    }
}
