using Application.Common.Persistence;
using Application.Features.Todos.Reminders.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityFramework.Projections;

/// <summary>Maps the reminder projection table.</summary>
public sealed class TodoReminderReadModelConfiguration : IEntityTypeConfiguration<TodoReminderModel>
{
    public void Configure(EntityTypeBuilder<TodoReminderModel> builder)
    {
        builder.ToTable("todo_reminders", PersistenceConstant.Schema.Todo);
        builder.HasKey(model => model.Id);
    }
}
