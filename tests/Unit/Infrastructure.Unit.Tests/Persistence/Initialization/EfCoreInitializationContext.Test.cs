using System.Data;

using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.UnitOfWorks;

using Infrastructure.Persistence.Initialization;

using Microsoft.Extensions.Logging;

namespace Infrastructure.Unit.Tests.Persistence.Initialization;

/// <summary>Unit tests for EfCoreInitializationContext.</summary>
public sealed class EfCoreInitializationContextTest
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var connection = new Mock<IDbConnection>().Object;
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger>().Object;
        var cancellationToken = CancellationToken.None;

        // Act
        var context = new EfCoreInitializationContext(connection, unitOfWork, logger, cancellationToken);

        // Assert
        context.Connection.Should().BeSameAs(connection);
        context.UnitOfWork.Should().BeSameAs(unitOfWork);
        context.Logger.Should().BeSameAs(logger);
        context.CancellationToken.Should().Be(cancellationToken);
        context.Items.Should().BeEmpty();
        context.Items.Should().BeOfType<Dictionary<string, object>>();
    }

    [Fact]
    public void Constructor_WithCancellationToken_ShouldPreserveToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var connection = new Mock<System.Data.IDbConnection>().Object;
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger>().Object;

        // Act
        var context = new EfCoreInitializationContext(connection, unitOfWork, logger, token);

        // Assert
        context.CancellationToken.Should().Be(token);
        context.CancellationToken.IsCancellationRequested.Should().BeFalse();

        // Act - cancel
        cts.Cancel();

        // Assert
        context.CancellationToken.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public void Items_ShouldAllowAddingAndRetrievingValues()
    {
        // Arrange
        var connection = new Mock<IDbConnection>().Object;
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger>().Object;
        var context = new EfCoreInitializationContext(connection, unitOfWork, logger);

        // Act
        context.Items["Key1"] = "Value1";
        context.Items["Key2"] = 42;
        context.Items["Key3"] = new[] { 1, 2, 3 };

        // Assert
        context.Items["Key1"].Should().Be("Value1");
        context.Items["Key2"].Should().Be(42);
        context.Items["Key3"].Should().BeEquivalentTo(new[] { 1, 2, 3 });
        context.Items.Count.Should().Be(3);
    }

    [Fact]
    public void Constructor_WithNullConnection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IDbConnection? connection = null;
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger>().Object;

        // Act & Assert
        var act = () => new EfCoreInitializationContext(connection!, unitOfWork, logger);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("connection");
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = new Mock<IDbConnection>().Object;
        IUnitOfWork? unitOfWork = null;
        var logger = new Mock<ILogger>().Object;

        // Act & Assert
        var act = () => new EfCoreInitializationContext(connection, unitOfWork!, logger);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("unitOfWork");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = new Mock<IDbConnection>().Object;
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        ILogger? logger = null;

        // Act & Assert
        var act = () => new EfCoreInitializationContext(connection, unitOfWork, logger!);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Items_ShouldSupportDifferentValueTypes()
    {
        // Arrange
        var connection = new Mock<IDbConnection>().Object;
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger>().Object;
        var context = new EfCoreInitializationContext(connection, unitOfWork, logger);

        // Act
        context.Items["String"] = "test";
        context.Items["Int"] = 123;
        context.Items["Bool"] = true;
        context.Items["DateTime"] = new DateTime(2024, 1, 1);
        context.Items["List"] = new List<int> { 1, 2, 3 };
        context.Items["Nested"] = new Dictionary<string, int> { ["a"] = 1 };

        // Assert
        context.Items.Count.Should().Be(6);
        context.Items["String"].Should().BeOfType<string>();
        context.Items["Int"].Should().BeOfType<int>();
        context.Items["Bool"].Should().BeOfType<bool>();
        context.Items["DateTime"].Should().BeOfType<DateTime>();
        context.Items["List"].Should().BeOfType<List<int>>();
        context.Items["Nested"].Should().BeOfType<Dictionary<string, int>>();
    }

    [Fact]
    public void Items_ShouldAllowOverwritingValues()
    {
        // Arrange
        var connection = new Mock<IDbConnection>().Object;
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger>().Object;
        var context = new EfCoreInitializationContext(connection, unitOfWork, logger);

        // Act
        context.Items["Key"] = "Original";
        context.Items["Key"] = "Updated";

        // Assert
        context.Items["Key"].Should().Be("Updated");
        context.Items.Count.Should().Be(1);
    }

    [Fact]
    public void Items_ShouldAllowRemovingValues()
    {
        // Arrange
        var connection = new Mock<IDbConnection>().Object;
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger>().Object;
        var context = new EfCoreInitializationContext(connection, unitOfWork, logger);
        context.Items["Key1"] = "Value1";
        context.Items["Key2"] = "Value2";

        // Act
        context.Items.Remove("Key1");

        // Assert
        context.Items.Should().ContainKey("Key2");
        context.Items.Should().NotContainKey("Key1");
        context.Items.Count.Should().Be(1);
    }

    [Fact]
    public void Context_ShouldImplementIInitializationContext()
    {
        // Arrange
        var connection = new Mock<IDbConnection>().Object;
        var unitOfWork = new Mock<IUnitOfWork>().Object;
        var logger = new Mock<ILogger>().Object;

        // Act
        var context = new EfCoreInitializationContext(connection, unitOfWork, logger);

        // Assert
        context.Should().BeAssignableTo<IInitializationContext>();
    }
}
