using Application;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

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
        using WebApplication app = BuildApplication(autoMigrate: true, seed: true);

        // Act
        WebApplication result = app.UseInfrastructure();

        // Assert
        result.Should().BeSameAs(app);
        (await _fixture.TableExistsAsync("todos")).Should().BeTrue();
        (await _fixture.CountRowsAsync("todo_categories")).Should().Be(4);
        (await _fixture.CountRowsAsync("todo_lists")).Should().Be(3);
        (await _fixture.CountRowsAsync("todos")).Should().Be(5);
    }

    [Fact(DisplayName = "UseInfrastructure with autoMigrate only should leave seed tables empty")]
    public async Task UseInfrastructure_WithAutoMigrateOnly_ShouldNotSeed()
    {
        // Arrange
        using WebApplication app = BuildApplication(autoMigrate: true, seed: false);

        // Act
        WebApplication result = app.UseInfrastructure();

        // Assert
        result.Should().BeSameAs(app);
        (await _fixture.TableExistsAsync("todos")).Should().BeTrue();
        (await _fixture.CountRowsAsync("todos")).Should().Be(0);
        (await _fixture.CountRowsAsync("__EFMigrationsHistory")).Should().Be(7);
    }

    [Fact(DisplayName = "UseInfrastructure with both flags false should not touch the database")]
    public void UseInfrastructure_WithNothingEnabled_ShouldNotTouchTheDatabase()
    {
        // Arrange
        using WebApplication app = BuildApplication();

        // Act
        WebApplication result = app.UseInfrastructure();

        // Assert
        result.Should().BeSameAs(app);
    }

    private WebApplication BuildApplication(bool autoMigrate = false, bool seed = false)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = _fixture.ConnectionString,
            ["EfCore:ConnectionStringName"] = "Default",
            ["EfCore:Provider"] = "PostgreSQL",
            ["Caching:Enabled"] = "false",
            ["Messaging:Enabled"] = "false",
            ["Outbox:Enabled"] = "false",
            ["Persistence:AutoMigrate"] = autoMigrate.ToString(),
            ["Persistence:Seed"] = seed.ToString(),
        });
        builder.AddApplication();
        builder.AddInfrastructure();
        return builder.Build();
    }
}