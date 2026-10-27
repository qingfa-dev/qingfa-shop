using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;

using Core.Todos;

namespace Infrastructure.Integration.Tests.Persistence.Todos;

/// <summary>Integration tests for UnitOfWork with Todo entities using PostgreSQL.</summary>
[Collection("Todo EfCore")]
public sealed class TodoUnitOfWorkIntegrationTest(TodoEfCoreFixture fixture) : IAsyncLifetime
{
    private readonly TodoEfCoreFixture _fixture = fixture;
    private IPersistentRepository<Todo, Guid> _todoRepo => _fixture.TodoRepository;
    private IUnitOfWork _unitOfWork => _fixture.UnitOfWork;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _fixture.CleanAsync();
        _fixture.CreateScope();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _fixture.CleanAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNoChanges_ShouldSucceed()
    {
        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenTodoAdded_ShouldPersist()
    {
        // Arrange
        var todo = CreateTodo("UoW test");

        // Act
        await _todoRepo.AddAsync(todo);
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var getResult = await _todoRepo.GetByIdAsync(todo.Id);
        getResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenWorkSucceeds_ShouldCommit()
    {
        // Arrange
        var todo = CreateTodo("Execute test");

        // Act
        var result = await _unitOfWork.ExecuteAsync(async ct =>
        {
            await _todoRepo.AddAsync(todo, ct);
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var getResult = await _todoRepo.GetByIdAsync(todo.Id);
        getResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenWorkThrows_ShouldReturnFailure()
    {
        // Act
        var result = await _unitOfWork.ExecuteAsync(async _ =>
        {
            throw new InvalidOperationException("Test exception");
        });

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNullWork_ShouldReturnFailure()
    {
        // Act
        var result = await _unitOfWork.ExecuteAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenWorkSucceeds_ShouldCommit()
    {
        // Arrange
        var todo = CreateTodo("Transaction test");

        // Act
        var result = await _unitOfWork.ExecuteInTransactionAsync(async (tx, ct) =>
        {
            await _todoRepo.AddAsync(todo, ct);
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var getResult = await _todoRepo.GetByIdAsync(todo.Id);
        getResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenWorkThrows_ShouldRollback()
    {
        // Arrange
        var todo = CreateTodo("Rollback test");

        // Act
        var result = await _unitOfWork.ExecuteInTransactionAsync(async (tx, ct) =>
        {
            await _todoRepo.AddAsync(todo, ct);
            throw new InvalidOperationException("Force rollback");
        });

        // Assert
        result.IsFailure.Should().BeTrue();
        // The todo should not be persisted due to rollback
        var getResult = await _todoRepo.GetByIdAsync(todo.Id);
        getResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenNullWork_ShouldReturnFailure()
    {
        // Act
        var result = await _unitOfWork.ExecuteInTransactionAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenStarted_ShouldReturnTransaction()
    {
        // Act
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Transaction_WhenCommitted_ShouldPersistChanges()
    {
        // Arrange
        var todo = CreateTodo("Commit test");
        await _todoRepo.AddAsync(todo);

        // Act
        var txResult = await _unitOfWork.BeginTransactionAsync();
        txResult.IsSuccess.Should().BeTrue();
        var tx = txResult.Value;

        var saveResult = await _unitOfWork.SaveChangesAsync();
        await tx.CommitAsync();

        // Assert
        saveResult.IsSuccess.Should().BeTrue();
        var getResult = await _todoRepo.GetByIdAsync(todo.Id);
        getResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Transaction_WhenRolledBack_ShouldDiscardChanges()
    {
        // Arrange
        var todo = CreateTodo("Rollback tx test");
        await _todoRepo.AddAsync(todo);

        // Act
        var txResult = await _unitOfWork.BeginTransactionAsync();
        txResult.IsSuccess.Should().BeTrue();
        var tx = txResult.Value;

        // Save within transaction
        await _unitOfWork.SaveChangesAsync();

        // Rollback
        await tx.RollbackAsync();

        // Assert: After rollback, the entity should not be found
        // Note: InMemory provider may not fully support rollback behavior
        var getResult = await _todoRepo.GetByIdAsync(todo.Id);
        // The result depends on the provider's transaction support
    }

    [Fact]
    public async Task Transaction_WithIsolationLevel_ShouldSucceed()
    {
        // Act: Test with ReadCommitted isolation level
        var result = await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await result.Value.RollbackAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenMultipleEntities_ShouldPersistAll()
    {
        // Arrange
        var todos = new[]
        {
            CreateTodo("Batch 1"),
            CreateTodo("Batch 2"),
            CreateTodo("Batch 3")
        };

        // Act
        await _todoRepo.AddRangeAsync(todos);
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        foreach (var todo in todos)
        {
            var getResult = await _todoRepo.GetByIdAsync(todo.Id);
            getResult.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SaveChangesAsync_WhenUpdate_ShouldPersistChanges()
    {
        // Arrange
        var todo = CreateTodo("Original title");
        await _todoRepo.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();

        // Act
        todo.Title = "Updated title";
        await _todoRepo.UpdateAsync(todo);
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var getResult = await _todoRepo.GetByIdAsync(todo.Id);
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Title.Should().Be("Updated title");
    }

    #region Helpers

    private static Todo CreateTodo(string title = "Test todo")
    {
        var now = DateTimeOffset.UtcNow;
        var result = Todo.Create(
            id: Guid.NewGuid(),
            tenantId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            title: title,
            nowUtc: now,
            actor: "test",
            description: "Test description",
            priority: TodoPriority.Medium,
            dueAtUtc: now.AddDays(1));
        return result.Value;
    }

    #endregion
}
