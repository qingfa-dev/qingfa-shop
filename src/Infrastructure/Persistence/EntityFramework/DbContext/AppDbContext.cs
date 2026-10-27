using Core.Todos;

using Infrastructure.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// Single EF Core DbContext for the entire application.
/// Manages persistence for all aggregates: Todo, TodoList, and TodoCategory.
/// </summary>
public sealed class ApplicationDbContext : EfCoreDbContext
{
    /// <summary>Gets the set of todo items.</summary>
    public DbSet<Todo> Todos => Set<Todo>();

    /// <summary>Gets the set of todo lists.</summary>
    public DbSet<TodoList> TodoLists => Set<TodoList>();

    /// <summary>Gets the set of todo categories.</summary>
    public DbSet<TodoCategory> TodoCategories => Set<TodoCategory>();

    /// <summary>Creates a new AppDbContext with the given options.</summary>
    /// <param name="options">The EF Core DbContext options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
