using Core.Todos;

using Infrastructure.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Todos;

/// <summary>
/// EF Core DbContext for the Todos module.
/// Manages persistence for Todo, TodoList, and TodoCategory aggregates.
/// </summary>
public sealed class TodoDbContext : EfCoreDbContext
{
    /// <summary>Gets the set of todo items.</summary>
    public DbSet<Todo> Todos => Set<Todo>();

    /// <summary>Gets the set of todo lists.</summary>
    public DbSet<TodoList> TodoLists => Set<TodoList>();

    /// <summary>Gets the set of todo categories.</summary>
    public DbSet<TodoCategory> TodoCategories => Set<TodoCategory>();

    /// <summary>Creates a new TodoDbContext with the given options.</summary>
    /// <param name="options">The EF Core DbContext options.</param>
    public TodoDbContext(DbContextOptions<TodoDbContext> options)
        : base(options)
    {
    }
}

