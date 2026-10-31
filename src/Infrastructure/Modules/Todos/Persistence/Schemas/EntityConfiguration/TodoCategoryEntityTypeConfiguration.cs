using Application.Modules.Todos.Persistence;

using Core.Modules.Todos.Categories;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Conventions;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Modules.Todos.Persistence.Schemas.EntityConfiguration;

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
        builder.ToTable("todo_categories", TodoPersistenceSchemaCatalog.SchemaName);

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

        // Enforce: Optimistic token bumped on every update.
        builder.Property(t => t.Version)
            .HasColumnName("version");

        // Indexes: Query performance for common filters.
        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => t.CreatedAtUtc);
        builder.HasIndex(t => new { t.TenantId, t.IsDeleted });
    }

}