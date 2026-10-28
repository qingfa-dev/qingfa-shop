using Core.Todos.Items;

using Infrastructure.Persistence.EntityFramework.Conventions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Schemas.Todos.EntityConfiguration;

/// <summary>
/// Configures the EF Core mapping for the Todo entity.
/// Defines columns, constraints, indexes, and JSON serialization for category IDs.
/// </summary>
public sealed class TodoEntityTypeConfiguration : IEntityTypeConfiguration<Todo>
{
    /// <summary>Applies the mapping configuration for the Todo entity.</summary>
    /// <param name="builder">The entity type builder for Todo.</param>
    public void Configure(EntityTypeBuilder<Todo> builder)
    {
        // Table: Use snake_case naming convention consistent with the project.
        builder.ToTable("todos");

        // Key: Primary key is the Guid identifier.
        builder.HasKey(t => t.Id);

        // Identity: Core properties
        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(TodoConstant.Constraints.Title.MaxLength)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(TodoConstant.Constraints.Description.MaxLength);

        // State: Status and priority as integer columns.
        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasEnumConversion<TodoStatus>()
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasColumnName("priority")
            .HasEnumConversion<TodoPriority>()
            .IsRequired();

        // Dates: UTC timestamps with precision.
        builder.Property(t => t.DueAtUtc)
            .HasColumnName("due_at_utc");

        builder.Property(t => t.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        // Relationships: Foreign key to TodoList.
        builder.Property(t => t.ListId)
            .HasColumnName("list_id");

        // Categories: Store as JSON array of GUIDs.
        builder.Property(t => t.CategoryIds)
            .HasColumnName("category_ids")
            .HasGuidListConversion();

        // Lifecycle: Creation audit fields.
        builder.Property(t => t.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(256);

        builder.Property(t => t.ModifiedAtUtc)
            .HasColumnName("modified_at_utc");

        builder.Property(t => t.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(256);

        // Soft delete: Deletion audit fields.
        builder.Property(t => t.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(t => t.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");

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
            .HasMetadataConversion();

        // Indexes: Query performance for common filters.
        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => t.ListId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.CreatedAtUtc);
        builder.HasIndex(t => t.DueAtUtc);

        // Composite: Tenant + soft delete filter for common queries.
        builder.HasIndex(t => new { t.TenantId, t.IsDeleted });
    }

}
