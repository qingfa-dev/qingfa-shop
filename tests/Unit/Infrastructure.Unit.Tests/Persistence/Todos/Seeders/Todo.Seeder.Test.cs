using System.Data;

using Application.Common.Persistence.Initialization;

using Core.Todos.Categories;
using Core.Todos.Items;
using Core.Todos.Lists;

using Infrastructure.Persistence.Schemas.Todos.Seeders;

using Microsoft.Extensions.Logging;

namespace Infrastructure.Unit.Tests.Persistence.Todos.Seeders;

/// <summary>Unit tests for TodoCategorySeeder.</summary>
public sealed class TodoCategorySeederTest
{
    [Fact]
    public void Name_ShouldReturnSeederName()
    {
        // Arrange
        var seeder = new TodoCategorySeeder();

        // Assert
        seeder.Name.Should().Be("seeder:todo-categories");
    }

    [Fact]
    public void DisplayName_ShouldReturnHumanReadableName()
    {
        // Arrange
        var seeder = new TodoCategorySeeder();

        // Assert
        seeder.DisplayName.Should().Be("Seed Todo Categories");
    }

    [Fact]
    public void Order_ShouldBeLow()
    {
        // Arrange
        var seeder = new TodoCategorySeeder();

        // Assert
        seeder.Order.Should().Be(10);
    }

    [Fact]
    public void DependsOn_ShouldBeEmpty()
    {
        // Arrange
        var seeder = new TodoCategorySeeder();

        // Assert
        seeder.DependsOn.Should().BeEmpty();
    }

    [Fact]
    public void IsRequired_ShouldBeFalse()
    {
        // Arrange
        var seeder = new TodoCategorySeeder();

        // Assert
        seeder.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void EntityType_ShouldReturnTodoCategory()
    {
        // Arrange
        var seeder = new TodoCategorySeeder();

        // Assert
        ((Application.Common.Persistence.Initialization.Seeders.IPersistenceSeeder)seeder).EntityType.Should().Be(typeof(TodoCategory));
    }

    [Fact]
    public void KeyType_ShouldReturnGuid()
    {
        // Arrange
        var seeder = new TodoCategorySeeder();

        // Assert
        ((Application.Common.Persistence.Initialization.Seeders.IPersistenceSeeder)seeder).KeyType.Should().Be(typeof(Guid));
    }

    [Fact]
    public async Task PreviewAsync_WhenEmptyDatabase_ShouldReturnInsertOperations()
    {
        // Arrange
        var connection = new Mock<IDbConnection>();
        var command = new Mock<IDbCommand>();
        connection.Setup(c => c.CreateCommand()).Returns(command.Object);
        command.Setup(c => c.ExecuteScalar()).Returns(0L);

        var context = new Mock<IInitializationContext>();
        context.Setup(c => c.Connection).Returns(connection.Object);
        context.Setup(c => c.Logger).Returns(Mock.Of<ILogger>());

        var seeder = new TodoCategorySeeder();

        // Act
        var result = await seeder.PreviewAsync(context.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(4);
        result.Value.All(op => op.Type == InitializationOperationType.DataChange).Should().BeTrue();
    }

    [Fact]
    public async Task PreviewAsync_WhenDataExists_ShouldReturnCheckOperation()
    {
        // Arrange
        var connection = new Mock<IDbConnection>();
        var command = new Mock<IDbCommand>();
        connection.Setup(c => c.CreateCommand()).Returns(command.Object);
        command.Setup(c => c.ExecuteScalar()).Returns(4L);

        var context = new Mock<IInitializationContext>();
        context.Setup(c => c.Connection).Returns(connection.Object);
        context.Setup(c => c.Logger).Returns(Mock.Of<ILogger>());

        var seeder = new TodoCategorySeeder();

        // Act
        var result = await seeder.PreviewAsync(context.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Type.Should().Be(InitializationOperationType.DataCheck);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDataExists_ShouldSkipSeeding()
    {
        // Arrange
        var connection = new Mock<IDbConnection>();
        var command = new Mock<IDbCommand>();
        connection.Setup(c => c.CreateCommand()).Returns(command.Object);
        command.Setup(c => c.ExecuteScalar()).Returns(4L);
        connection.Setup(c => c.State).Returns(ConnectionState.Closed);

        var context = new Mock<IInitializationContext>();
        context.Setup(c => c.Connection).Returns(connection.Object);
        context.Setup(c => c.Logger).Returns(Mock.Of<ILogger>());

        var seeder = new TodoCategorySeeder();

        // Act
        var result = await seeder.ExecuteAsync(context.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Verify no INSERT was executed
        command.Verify(c => c.ExecuteNonQuery(), Times.Never);
    }
}

/// <summary>Unit tests for TodoListSeeder.</summary>
public sealed class TodoListSeederTest
{
    [Fact]
    public void Name_ShouldReturnSeederName()
    {
        // Arrange
        var seeder = new TodoListSeeder();

        // Assert
        seeder.Name.Should().Be("seeder:todo-lists");
    }

    [Fact]
    public void DisplayName_ShouldReturnHumanReadableName()
    {
        // Arrange
        var seeder = new TodoListSeeder();

        // Assert
        seeder.DisplayName.Should().Be("Seed Todo Lists");
    }

    [Fact]
    public void Order_ShouldBeHigherThanCategories()
    {
        // Arrange
        var seeder = new TodoListSeeder();

        // Assert
        seeder.Order.Should().Be(20);
    }

    [Fact]
    public void DependsOn_ShouldBeEmpty()
    {
        // Arrange
        var seeder = new TodoListSeeder();

        // Assert
        seeder.DependsOn.Should().BeEmpty();
    }

    [Fact]
    public void EntityType_ShouldReturnTodoList()
    {
        // Arrange
        var seeder = new TodoListSeeder();

        // Assert
        ((Application.Common.Persistence.Initialization.Seeders.IPersistenceSeeder)seeder).EntityType.Should().Be(typeof(TodoList));
    }

    [Fact]
    public async Task PreviewAsync_WhenEmptyDatabase_ShouldReturnInsertOperations()
    {
        // Arrange
        var connection = new Mock<IDbConnection>();
        var command = new Mock<IDbCommand>();
        connection.Setup(c => c.CreateCommand()).Returns(command.Object);
        command.Setup(c => c.ExecuteScalar()).Returns(0L);

        var context = new Mock<IInitializationContext>();
        context.Setup(c => c.Connection).Returns(connection.Object);
        context.Setup(c => c.Logger).Returns(Mock.Of<ILogger>());

        var seeder = new TodoListSeeder();

        // Act
        var result = await seeder.PreviewAsync(context.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.All(op => op.Type == InitializationOperationType.DataChange).Should().BeTrue();
    }

    [Fact]
    public async Task PreviewAsync_WhenDataExists_ShouldReturnCheckOperation()
    {
        // Arrange
        var connection = new Mock<IDbConnection>();
        var command = new Mock<IDbCommand>();
        connection.Setup(c => c.CreateCommand()).Returns(command.Object);
        command.Setup(c => c.ExecuteScalar()).Returns(3L);

        var context = new Mock<IInitializationContext>();
        context.Setup(c => c.Connection).Returns(connection.Object);
        context.Setup(c => c.Logger).Returns(Mock.Of<ILogger>());

        var seeder = new TodoListSeeder();

        // Act
        var result = await seeder.PreviewAsync(context.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Type.Should().Be(InitializationOperationType.DataCheck);
    }
}

/// <summary>Unit tests for TodoSeeder.</summary>
public sealed class TodoSeederTest
{
    [Fact]
    public void Name_ShouldReturnSeederName()
    {
        // Arrange
        var seeder = new TodoSeeder();

        // Assert
        seeder.Name.Should().Be("seeder:todos");
    }

    [Fact]
    public void DisplayName_ShouldReturnHumanReadableName()
    {
        // Arrange
        var seeder = new TodoSeeder();

        // Assert
        seeder.DisplayName.Should().Be("Seed Todo Items");
    }

    [Fact]
    public void Order_ShouldBeHighest()
    {
        // Arrange
        var seeder = new TodoSeeder();

        // Assert
        seeder.Order.Should().Be(30);
    }

    [Fact]
    public void DependsOn_ShouldIncludeCategoriesAndLists()
    {
        // Arrange
        var seeder = new TodoSeeder();

        // Assert
        seeder.DependsOn.Should().Contain("seeder:todo-categories");
        seeder.DependsOn.Should().Contain("seeder:todo-lists");
    }

    [Fact]
    public void EntityType_ShouldReturnTodo()
    {
        // Arrange
        var seeder = new TodoSeeder();

        // Assert
        ((Application.Common.Persistence.Initialization.Seeders.IPersistenceSeeder)seeder).EntityType.Should().Be(typeof(Todo));
    }

    [Fact]
    public void KeyType_ShouldReturnGuid()
    {
        // Arrange
        var seeder = new TodoSeeder();

        // Assert
        ((Application.Common.Persistence.Initialization.Seeders.IPersistenceSeeder)seeder).KeyType.Should().Be(typeof(Guid));
    }

    [Fact]
    public async Task PreviewAsync_WhenEmptyDatabase_ShouldReturnInsertOperations()
    {
        // Arrange
        var connection = new Mock<IDbConnection>();
        var countCommand = new Mock<IDbCommand>();
        var catCommand = new Mock<IDbCommand>();
        var listCommand = new Mock<IDbCommand>();

        // First call returns count (0), subsequent calls return category/list IDs
        var callCount = 0;
        connection.Setup(c => c.CreateCommand())
            .Returns(() =>
            {
                callCount++;
                return callCount == 1 ? countCommand.Object : callCount == 2 ? catCommand.Object : listCommand.Object;
            });

        countCommand.Setup(c => c.ExecuteScalar()).Returns(0L);
        catCommand.Setup(c => c.ExecuteScalar()).Returns(Guid.NewGuid().ToString());
        listCommand.Setup(c => c.ExecuteScalar()).Returns(Guid.NewGuid().ToString());

        var context = new Mock<IInitializationContext>();
        context.Setup(c => c.Connection).Returns(connection.Object);
        context.Setup(c => c.Logger).Returns(Mock.Of<ILogger>());

        var seeder = new TodoSeeder();

        // Act
        var result = await seeder.PreviewAsync(context.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
        result.Value.All(op => op.Type == InitializationOperationType.DataChange).Should().BeTrue();
    }

    [Fact]
    public async Task PreviewAsync_WhenDataExists_ShouldReturnCheckOperation()
    {
        // Arrange
        var connection = new Mock<IDbConnection>();
        var command = new Mock<IDbCommand>();
        connection.Setup(c => c.CreateCommand()).Returns(command.Object);
        command.Setup(c => c.ExecuteScalar()).Returns(5L);

        var context = new Mock<IInitializationContext>();
        context.Setup(c => c.Connection).Returns(connection.Object);
        context.Setup(c => c.Logger).Returns(Mock.Of<ILogger>());

        var seeder = new TodoSeeder();

        // Act
        var result = await seeder.PreviewAsync(context.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Type.Should().Be(InitializationOperationType.DataCheck);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDataExists_ShouldSkipSeeding()
    {
        // Arrange
        var connection = new Mock<IDbConnection>();
        var command = new Mock<IDbCommand>();
        connection.Setup(c => c.CreateCommand()).Returns(command.Object);
        command.Setup(c => c.ExecuteScalar()).Returns(5L);

        var context = new Mock<IInitializationContext>();
        context.Setup(c => c.Connection).Returns(connection.Object);
        context.Setup(c => c.Logger).Returns(Mock.Of<ILogger>());

        var seeder = new TodoSeeder();

        // Act
        var result = await seeder.ExecuteAsync(context.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        command.Verify(c => c.ExecuteNonQuery(), Times.Never);
    }
}
