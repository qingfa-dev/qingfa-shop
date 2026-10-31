using Application.Modules.Todos.Items.Models;
using Application.Modules.Todos.Persistence;

using Core.Modules.Todos.Items;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Conventions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Modules.Todos.Persistence.Schemas.EntityConfiguration;

/// <summary>
/// Configures the EF Core mapping for the todo read model.
/// Keeps the existing <c>todos</c> table and column names so the projection replaces the
/// former aggregate mapping without a destructive migration.
/// </summary>
public sealed class TodoReadModelConfiguration : IEntityTypeConfiguration<TodoReadModel>
{
    /// <summary>Applies the mapping configuration for the todo read model.</summary>
    /// <param name="builder">The entity type builder for the read model.</param>
    public void Configure(EntityTypeBuilder<TodoReadModel> builder)
    {
        // Table: reuse the existing todos table in the module's schema.
        builder.ToTable("todos", TodoPersistenceSchemaCatalog.SchemaName);

        builder.HasKey(model => model.Id);

        // Identity: Core properties.
        builder.Property(model => model.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(model => model.Title)
            .HasColumnName("title")
            .HasMaxLength(TodoConstant.Constraints.Title.MaxLength)
            .IsRequired();

        builder.Property(model => model.Description)
            .HasColumnName("description")
            .HasMaxLength(TodoConstant.Constraints.Description.MaxLength);

        // State: Status and priority as integer columns.
        builder.Property(model => model.Status)
            .HasColumnName("status")
            .HasEnumConversion<TodoStatus>()
            .IsRequired();

        builder.Property(model => model.Priority)
            .HasColumnName("priority")
            .HasEnumConversion<TodoPriority>()
            .IsRequired();

        // Dates: UTC timestamps with precision.
        builder.Property(model => model.DueAtUtc)
            .HasColumnName("due_at_utc");

        builder.Property(model => model.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        // Relationships: Foreign key to TodoList.
        builder.Property(model => model.ListId)
            .HasColumnName("list_id");

        // Categories: Store as JSON array of GUIDs.
        builder.Property(model => model.CategoryIds)
            .HasColumnName("category_ids")
            .HasGuidListConversion();

        // Lifecycle: Creation audit fields.
        builder.Property(model => model.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(model => model.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(256);

        builder.Property(model => model.ModifiedAtUtc)
            .HasColumnName("modified_at_utc");

        builder.Property(model => model.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(256);

        // Soft delete: Deletion audit fields.
        builder.Property(model => model.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(model => model.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");

        builder.Property(model => model.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(256);

        // Tenancy: Tenant isolation.
        builder.Property(model => model.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        // Metadata: JSON storage for extensible metadata.
        builder.Property(model => model.Metadata)
            .HasColumnName("metadata")
            .HasJsonbConversion();

        // Enforce: Projection version marker; the projection guards replays with >, not a concurrency token.
        builder.Property(model => model.Version)
            .HasColumnName("version");

        // Computed: IsCompleted is derived from Status, never persisted.
        builder.Ignore(model => model.IsCompleted);

        // Indexes: Query performance for common filters.
        builder.HasIndex(model => model.TenantId);
        builder.HasIndex(model => model.ListId);
        builder.HasIndex(model => model.Status);
        builder.HasIndex(model => model.Priority);
        builder.HasIndex(model => model.CreatedAtUtc);
        builder.HasIndex(model => model.DueAtUtc);

        // Composite: Tenant + soft delete filter for common queries.
        builder.HasIndex(model => new { model.TenantId, model.IsDeleted });
    }
}