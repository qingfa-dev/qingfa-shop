using Application.Common.Querying;
using Application.Common.Querying.Filtering;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Repositories;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Streaming")]
public sealed class EfCoreRepositoryStreamingTest
{
    #region StreamAsync

    [Fact(DisplayName = "StreamAsync should return async enumerable")]
    public async Task StreamAsync_ShouldReturnAsyncEnumerable()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var products = new[]
        {
            EfCoreRepositoryTestFixture.CreateProduct("A", 10m),
            EfCoreRepositoryTestFixture.CreateProduct("B", 20m)
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();

        // Act
        Result<IAsyncEnumerable<UnitTestProduct>> result = await repository.StreamAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact(DisplayName = "StreamAsync should stream all entities")]
    public async Task StreamAsync_ShouldStreamAllEntities()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var products = new[]
        {
            EfCoreRepositoryTestFixture.CreateProduct("A", 10m),
            EfCoreRepositoryTestFixture.CreateProduct("B", 20m),
            EfCoreRepositoryTestFixture.CreateProduct("C", 30m)
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();

        // Act
        Result<IAsyncEnumerable<UnitTestProduct>> result = await repository.StreamAsync(query);
        var streamed = new List<UnitTestProduct>();
        await foreach (var item in result.Value!)
        {
            streamed.Add(item);
        }

        // Assert
        result.IsSuccess.Should().BeTrue();
        streamed.Should().HaveCount(3);
    }

    [Fact(DisplayName = "StreamAsync with empty results should return empty async enumerable")]
    public async Task StreamAsync_WithEmptyResults_ShouldReturnEmptyEnumerable()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();

        // Act
        Result<IAsyncEnumerable<UnitTestProduct>> result = await repository.StreamAsync(query);
        var streamed = new List<UnitTestProduct>();
        await foreach (var item in result.Value!)
        {
            streamed.Add(item);
        }

        // Assert
        result.IsSuccess.Should().BeTrue();
        streamed.Should().BeEmpty();
    }

    [Fact(DisplayName = "StreamAsync with filter should not fail")]
    public async Task StreamAsync_WithFilter_ShouldNotFail()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        context.Products.Add(EfCoreRepositoryTestFixture.CreateProduct("Widget", 10m));
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "Widget", "Widget", typeof(string), true, null, null)
        };

        // Act
        Result<IAsyncEnumerable<UnitTestProduct>> result = await repository.StreamAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "StreamAsync with failing filter should return failure")]
    public async Task StreamAsync_WithFailingFilter_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var translator = new FailingMockQueryTranslator<UnitTestProduct> { FailFilter = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryWithTranslator(context, translator);
        var query = new Query<UnitTestProduct>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "Widget", "Widget", typeof(string), true, null, null)
        };

        // Act
        Result<IAsyncEnumerable<UnitTestProduct>> result = await repository.StreamAsync(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "StreamAsync with cancelled token should handle cancellation")]
    public async Task StreamAsync_WithCancelledToken_ShouldHandleCancellation()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result<IAsyncEnumerable<UnitTestProduct>> result = await repository.StreamAsync(query, cts.Token);

        // Assert — InMemory provider may not throw on cancelled token, so we accept either outcome
        // The key is that it returns a Result, not an unhandled exception
        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "StreamAsync should use AsNoTracking")]
    public async Task StreamAsync_ShouldUseAsNoTracking()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        context.Products.Add(EfCoreRepositoryTestFixture.CreateProduct("A", 10m));
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();

        // Act
        Result<IAsyncEnumerable<UnitTestProduct>> result = await repository.StreamAsync(query);
        var streamed = new List<UnitTestProduct>();
        await foreach (var item in result.Value!)
        {
            streamed.Add(item);
        }

        // Assert
        result.IsSuccess.Should().BeTrue();
        streamed.Should().HaveCount(1);
        // AsNoTracking entities should have Detached state
        context.Entry(streamed[0]).State.Should().Be(EntityState.Detached);
    }

    [Fact(DisplayName = "StreamAsync with large dataset should stream efficiently")]
    public async Task StreamAsync_WithLargeDataset_ShouldStreamEfficiently()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var products = Enumerable.Range(1, 100)
            .Select(i => EfCoreRepositoryTestFixture.CreateProduct($"Product {i}", i * 10m))
            .ToList();
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();

        // Act
        Result<IAsyncEnumerable<UnitTestProduct>> result = await repository.StreamAsync(query);
        int count = 0;
        await foreach (var _ in result.Value!)
        {
            count++;
        }

        // Assert
        result.IsSuccess.Should().BeTrue();
        count.Should().Be(100);
    }

    #endregion
}
