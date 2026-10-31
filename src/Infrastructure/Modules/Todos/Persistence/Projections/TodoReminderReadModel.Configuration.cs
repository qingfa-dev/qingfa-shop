using Application.Modules.Todos.Persistence;
using Application.Modules.Todos.Reminders.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Modules.Todos.Persistence.Projections;

/// <summary>Maps the reminder projection table.</summary>
public sealed class TodoReminderReadModelConfiguration : IEntityTypeConfiguration<TodoReminderModel>
{
    public void Configure(EntityTypeBuilder<TodoReminderModel> builder)
    {
        builder.ToTable("todo_reminders", TodoPersistenceSchemaCatalog.SchemaName);
        builder.HasKey(model => model.Id);
    }
}
