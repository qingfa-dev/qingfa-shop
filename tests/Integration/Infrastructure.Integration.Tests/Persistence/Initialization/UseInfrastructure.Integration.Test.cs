using Infrastructure;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Todos;

using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Integration.Tests.Persistence.Initialization;

/// <summary>
/// Integration tests for the <c>UseInfrastructure</c> startup hook driving the
/// full migration and seeding pipeline against PostgreSQL.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Infrastructure.Composition.UseInfrastructure.PostgreSQL")]
[Collection(PersistenceInitializationCollection.Name)]
public sealed class UseInfrastructureIntegrationTest(PersistenceInitializationFixture fixture) : IAsyncLifetime
{
    private readonly PersistenceInitializationFixture _fixture = fixture;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _fixture.DropDatabaseAsync();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact(DisplayName = "UseInfrastructure with autoMigrate and seed should migrate and seed")]
    public async Task UseInfrastructure_WithAutoMigrateAndSeed_ShouldMigrateAndSeed()
    {
        // Arrange
        await using var provider = BuildProvider();

        // Act
        var result = provider.UseInfrastructure(autoMigrate: true, seed: true);

        // Assert
        result.Should().BeSameAs(provider);
        (await _fixture.TableExistsAsync("todos")).Should().BeTrue();
        (await _fixture.CountRowsAsync("todo_categories")).Should().Be(4);
        (await _fixture.CountRowsAsync("todo_lists")).Should().Be(3);
        (await _fixture.CountRowsAsync("todos")).Should().Be(5);
    }

    [Fact(DisplayName = "UseInfrastructure with autoMigrate only should leave seed tables empty")]
    public async Task UseInfrastructure_WithAutoMigrateOnly_ShouldNotSeed()
    {
        // Arrange
        await using var provider = BuildProvider();

        // Act
        var result = provider.UseInfrastructure(autoMigrate: true, seed: false);

        // Assert
        result.Should().BeSameAs(provider);
        (await _fixture.TableExistsAsync("todos")).Should().BeTrue();
        (await _fixture.CountRowsAsync("todos")).Should().Be(0);
        (await _fixture.CountRowsAsync("__EFMigrationsHistory")).Should().Be(1);
    }

    [Fact(DisplayName = "UseInfrastructure with both flags false should not touch the database")]
    public void UseInfrastructure_WithNothingEnabled_ShouldNotTouchTheDatabase()
    {
        // Arrange
        using var provider = BuildProvider();

        // Act
        var result = provider.UseInfrastructure(autoMigrate: false, seed: false);

        // Assert
        result.Should().BeSameAs(provider);
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPersistence<ApplicationDbContext>(_fixture.ConnectionString);
        return services.BuildServiceProvider();
    }
}
