using Application.Common.Caching;
using Application.Common.Persistence.Repositories;
using Application.Common.Querying.Mapping;

using Core.Todos.Categories;
using Core.Todos.Items;
using Core.Todos.Lists;

using Infrastructure.Caching.Services;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Repositories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Unit.Tests.Common.Composition;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "InfrastructureComposition")]
public class InfrastructureCompositionTest
{
    [Fact(DisplayName = "AddInfrastructure with connection string should register EF Core services")]
    public void AddInfrastructure_WithConnectionString_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var connectionString = "Host=localhost;Database=test;Username=test;Password=test";

        // Act
        services.AddInfrastructure(connectionString);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<QueryMapRegistry>().Should().NotBeNull();
        serviceProvider.GetService<ICacheService>().Should().NotBeNull();
        serviceProvider.GetService<ICacheService>().Should().BeOfType<NullCacheService>();
    }

    [Fact(DisplayName = "AddInfrastructure should throw when services is null")]
    public void AddInfrastructure_WithNullServices_ShouldThrow()
    {
        // Arrange
        IServiceCollection? services = null;
        var connectionString = "Host=localhost;Database=test";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)services!).AddInfrastructure(connectionString));
    }

    [Fact(DisplayName = "AddInfrastructure should throw when connection string is null")]
    public void AddInfrastructure_WithNullConnectionString_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        string? connectionString = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddInfrastructure(connectionString!));
    }

    [Fact(DisplayName = "AddInfrastructure should throw when connection string is empty")]
    public void AddInfrastructure_WithEmptyConnectionString_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => services.AddInfrastructure(""));
    }

    [Fact(DisplayName = "AddInfrastructure should register open-generic repositories")]
    public void AddInfrastructure_ShouldRegisterRepositories()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddInfrastructure("Host=localhost;Database=test");

        // Assert
        var persistentRepo = services.FirstOrDefault(s =>
            s.ServiceType == typeof(IPersistentRepository<,>));
        persistentRepo.Should().NotBeNull();
        persistentRepo!.ImplementationType.Should().Be(typeof(EfCoreRepository<,>));

        var projectionRepo = services.FirstOrDefault(s =>
            s.ServiceType == typeof(IProjectionRepository<,>));
        projectionRepo.Should().NotBeNull();
        projectionRepo!.ImplementationType.Should().Be(typeof(EfCoreRepository<,>));

        var streamableRepo = services.FirstOrDefault(s =>
            s.ServiceType == typeof(IStreamableRepository<,>));
        streamableRepo.Should().NotBeNull();
        streamableRepo!.ImplementationType.Should().Be(typeof(EfCoreRepository<,>));
    }

    [Fact(DisplayName = "AddInfrastructure should register QueryMapRegistry with Todo maps")]
    public void AddInfrastructure_ShouldRegisterQueryMaps()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddInfrastructure("Host=localhost;Database=test");
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        var registry = serviceProvider.GetRequiredService<QueryMapRegistry>();
        registry.Count.Should().Be(3);
        registry.IsRegistered<Todo>().Should().BeTrue();
        registry.IsRegistered<TodoList>().Should().BeTrue();
        registry.IsRegistered<TodoCategory>().Should().BeTrue();
    }

    [Fact(DisplayName = "AddInfrastructure should auto-register every query map definition")]
    public void AddInfrastructure_ShouldAutoRegisterQueryMapDefinitions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var expected = typeof(ApplicationDbContext).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(IQueryMapDefinition).IsAssignableFrom(type))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Act
        services.AddInfrastructure("Host=localhost;Database=test");
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        var registered = serviceProvider.GetServices<IQueryMapDefinition>()
            .Select(definition => definition.GetType().Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Assert
        expected.Should().NotBeEmpty();
        registered.Should().Equal(expected);
    }

    [Fact(DisplayName = "UseInfrastructure should return the same provider for chaining")]
    public void UseInfrastructure_ShouldReturnSameProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure("Host=localhost;Database=test");
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act
        var result = serviceProvider.UseInfrastructure(autoMigrate: false);

        // Assert
        result.Should().BeSameAs(serviceProvider);
    }

    [Fact(DisplayName = "UseInfrastructure should throw when provider is null")]
    public void UseInfrastructure_WithNullProvider_ShouldThrow()
    {
        // Arrange
        IServiceProvider? provider = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => provider!.UseInfrastructure());
    }

    [Fact(DisplayName = "UseInfrastructure with autoMigrate false should skip migrations")]
    public void UseInfrastructure_WithAutoMigrateFalse_ShouldSkipMigrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure("Host=localhost;Database=test");
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act — autoMigrate: false avoids touching the database
        var action = () => serviceProvider.UseInfrastructure(autoMigrate: false);

        // Assert
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "AddInfrastructure with IConfiguration should register services")]
    public void AddInfrastructure_WithConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=localhost;Database=test",
            ["EfCore:ConnectionStringName"] = "Default",
            ["EfCore:Provider"] = "PostgreSQL",
            ["Caching:Enabled"] = "false"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddInfrastructure(configuration);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<QueryMapRegistry>().Should().NotBeNull();
    }

    [Fact(DisplayName = "AddInfrastructure with IConfiguration should throw when services is null")]
    public void AddInfrastructure_WithNullServicesAndConfig_ShouldThrow()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)services!).AddInfrastructure(configuration));
    }

    [Fact(DisplayName = "AddInfrastructure with IConfiguration should throw when configuration is null")]
    public void AddInfrastructure_WithNullConfiguration_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration? configuration = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddInfrastructure(configuration!));
    }
}
