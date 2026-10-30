using Application;
using Application.Common.Caching;
using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Resetters;
using Application.Common.Persistence.Repositories;
using Application.Common.Querying.Mapping;
using Application.Common.Security.Authentication.Contexts;
using Application.Features.Todos.Reminders.Models;

using Core.Todos.Categories;
using Application.Features.Todos.Items.Models;

using Core.Todos.Items;
using Core.Todos.Lists;

using Infrastructure.Caching.Services;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Repositories;
using Infrastructure.Security.Authentication.Contexts;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Unit.Tests.Common.Composition;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "InfrastructureComposition")]
public class InfrastructureCompositionTest
{
    [Fact(DisplayName = "AddInfrastructure should register EF Core, query map, and cache services")]
    public void AddInfrastructure_ShouldRegisterServices()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder();

        // Act
        WebApplicationBuilder returned = builder.AddInfrastructure();
        using ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        returned.Should().BeSameAs(builder);
        serviceProvider.GetService<QueryMapRegistry>().Should().NotBeNull();
        serviceProvider.GetService<ICacheService>().Should().BeOfType<NullCacheService>();
        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName != null &&
            descriptor.ServiceType.FullName.Contains("INotificationService", StringComparison.Ordinal));
        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName != null &&
            descriptor.ServiceType.FullName.Contains("IStorageService", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "AddInfrastructure should throw when builder is null")]
    public void AddInfrastructure_WithNullBuilder_ShouldThrow()
    {
        // Arrange
        WebApplicationBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddInfrastructure());
    }

    [Fact(DisplayName = "AddInfrastructure should register open-generic repositories")]
    public void AddInfrastructure_ShouldRegisterRepositories()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder();

        // Act
        builder.AddInfrastructure();

        // Assert
        var persistentRepo = builder.Services.FirstOrDefault(s =>
            s.ServiceType == typeof(IPersistentRepository<,>));
        persistentRepo.Should().NotBeNull();
        persistentRepo!.ImplementationType.Should().Be(typeof(EfCoreRepository<,>));

        var projectionRepo = builder.Services.FirstOrDefault(s =>
            s.ServiceType == typeof(IProjectionRepository<,>));
        projectionRepo.Should().NotBeNull();
        projectionRepo!.ImplementationType.Should().Be(typeof(EfCoreRepository<,>));

        var streamableRepo = builder.Services.FirstOrDefault(s =>
            s.ServiceType == typeof(IStreamableRepository<,>));
        streamableRepo.Should().NotBeNull();
        streamableRepo!.ImplementationType.Should().Be(typeof(EfCoreRepository<,>));
    }

    [Fact(DisplayName = "AddInfrastructure should register QueryMapRegistry with Todo maps")]
    public void AddInfrastructure_ShouldRegisterQueryMaps()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder();

        // Act
        builder.AddInfrastructure();
        using ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var registry = serviceProvider.GetRequiredService<QueryMapRegistry>();
        registry.Count.Should().Be(4);
        registry.IsRegistered<TodoReadModel>().Should().BeTrue();
        registry.IsRegistered<TodoList>().Should().BeTrue();
        registry.IsRegistered<TodoCategory>().Should().BeTrue();
        registry.IsRegistered<TodoReminderModel>().Should().BeTrue();
    }

    [Fact(DisplayName = "AddInfrastructure should auto-register every query map definition")]
    public void AddInfrastructure_ShouldAutoRegisterQueryMapDefinitions()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder();
        var expected = typeof(ApplicationDbContext).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(IQueryMapDefinition).IsAssignableFrom(type))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Act
        builder.AddInfrastructure();
        using ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
        var registered = serviceProvider.GetServices<IQueryMapDefinition>()
            .Select(definition => definition.GetType().Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Assert
        expected.Should().NotBeEmpty();
        registered.Should().Equal(expected);
    }

    [Fact(DisplayName = "AddInfrastructure should register the HTTP-backed request contexts")]
    public void AddInfrastructure_ShouldRegisterSecurityContexts()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder();

        // Act
        builder.AddInfrastructure();
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        // Assert
        scope.ServiceProvider.GetService<IHttpContextAccessor>().Should().NotBeNull();
        scope.ServiceProvider.GetService<ITenantContext>().Should().BeOfType<TenantContext>();
        scope.ServiceProvider.GetService<IUserContext>().Should().BeOfType<CurrentUser>();
    }

    [Fact(DisplayName = "UseInfrastructure should return the same application for chaining")]
    public void UseInfrastructure_ShouldReturnSameApplication()
    {
        // Arrange
        using WebApplication app = CreateApplication();

        // Act
        WebApplication result = app.UseInfrastructure();

        // Assert
        result.Should().BeSameAs(app);
    }

    [Fact(DisplayName = "UseInfrastructure should throw when application is null")]
    public void UseInfrastructure_WithNullApplication_ShouldThrow()
    {
        // Arrange
        WebApplication? app = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => app!.UseInfrastructure());
    }

    [Fact(DisplayName = "UseInfrastructure should skip persistence when the host has no opt-in")]
    public void UseInfrastructure_WithoutConfiguration_ShouldSkipPersistence()
    {
        // Arrange
        using WebApplication app = CreateApplication();

        // Act
        var action = () => app.UseInfrastructure();

        // Assert
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "UseInfrastructure should run the persistence pipeline when enabled")]
    public void UseInfrastructure_WithAutoMigrate_ShouldRunInitializer()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder(autoMigrate: true);
        builder.AddApplication();
        builder.AddInfrastructure();
        var initializer = new Mock<IPersistenceInitializer>();
        initializer
            .Setup(value => value.InitializeAsync(
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<ResetScope>(), It.IsAny<IProgress<InitializationProgress>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        builder.Services.RemoveAll<IPersistenceInitializer>();
        builder.Services.AddScoped(_ => initializer.Object);

        using WebApplication app = builder.Build();

        // Act
        app.UseInfrastructure();

        // Assert
        initializer.Verify(value => value.InitializeAsync(
            true, false, false,
            It.IsAny<ResetScope>(), It.IsAny<IProgress<InitializationProgress>?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static WebApplicationBuilder CreateBuilder(bool autoMigrate = false)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["EfCore:ConnectionStringName"] = "Default",
            ["EfCore:Provider"] = "PostgreSQL",
            ["Caching:Enabled"] = "false",
            ["Persistence:AutoMigrate"] = autoMigrate.ToString(),
            ["Persistence:Seed"] = "false",
        });
        return builder;
    }

    private static WebApplication CreateApplication()
    {
        WebApplicationBuilder builder = CreateBuilder();
        builder.AddApplication();
        builder.AddInfrastructure();
        return builder.Build();
    }
}
