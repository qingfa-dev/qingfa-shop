using Application.Common.EventSourcing;
using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Seeders;
using Application.Common.Persistence.UnitOfWorks;
using Application.Features.Todos.Items.Models;

using Core.Todos.Categories;
using Core.Todos.Items;
using Core.Todos.Lists;

using Infrastructure.Persistence;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.EventSourcing.Options;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;
using Infrastructure.Persistence.Initialization;
using Infrastructure.Security.Identity.Domain.Roles;
using Infrastructure.Security.Identity.Domain.Users;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Unit.Tests.Persistence.Initialization;

/// <summary>
/// Unit tests for the persistence initialization composition root.
/// </summary>
public sealed class PersistenceInitializationCompositionTest
{
    [Fact(DisplayName = "AddPersistenceInitialization should register the initializer")]
    public void AddPersistenceInitialization_ShouldRegisterInitializer()
    {
        // Arrange
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        // Act
        var initializer = scope.ServiceProvider.GetService<IPersistenceInitializer>();

        // Assert
        initializer.Should().NotBeNull();
        initializer.Should().BeOfType<EfCorePersistenceInitializer<ApplicationDbContext>>();
    }

    [Fact(DisplayName = "AddPersistenceInitialization should register every seeder as a step")]
    public void AddPersistenceInitialization_ShouldRegisterSeederSteps()
    {
        // Arrange
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        // Act
        var steps = scope.ServiceProvider.GetServices<IInitializationStep>().ToList();

        // Assert
        steps.Should().HaveCount(6);
        steps.Select(step => step.Name).Should().BeEquivalentTo(
            "seeder:identity-roles",
            "seeder:identity-users",
            "seeder:todo-categories",
            "seeder:todo-lists",
            "seeder:todos",
            "seeder:todo-backfill");
    }

    [Fact(DisplayName = "AddPersistenceInitialization should register every persistence seeder")]
    public void AddPersistenceInitialization_ShouldRegisterPersistenceSeeders()
    {
        // Arrange
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        // Act
        var seeders = scope.ServiceProvider.GetServices<IPersistenceSeeder>().ToList();

        // Assert
        seeders.Should().HaveCount(6);
        seeders.Select(seeder => seeder.EntityType).Should().BeEquivalentTo(
            new[]
            {
                typeof(Role),
                typeof(User),
                typeof(TodoCategory),
                typeof(TodoList),
                typeof(Todo),
                typeof(TodoReadModel)
            });
    }

    [Fact(DisplayName = "AddPersistenceInitialization should make the non-generic ILogger resolvable")]
    public void AddPersistenceInitialization_ShouldRegisterNonGenericLogger()
    {
        // Arrange
        using var provider = BuildProvider();

        // Act
        var logger = provider.GetService<ILogger>();

        // Assert
        logger.Should().NotBeNull();
    }

    [Fact(DisplayName = "AddPersistenceInitialization should not duplicate registrations")]
    public void AddPersistenceInitialization_CalledTwice_ShouldNotDuplicateSteps()
    {
        // Arrange
        WebApplicationBuilder builder = CreateBuilder();
        IServiceCollection services = builder.Services;
        builder.AddPersistenceInitialization<ApplicationDbContext>();
        builder.AddPersistenceInitialization<ApplicationDbContext>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var steps = scope.ServiceProvider.GetServices<IInitializationStep>().ToList();

        // Assert
        steps.Should().HaveCount(6);
    }

    [Fact(DisplayName = "AddPersistenceInitialization should register the initializer as scoped")]
    public void AddPersistenceInitialization_ShouldRegisterScopedInitializer()
    {
        // Arrange
        using var provider = BuildProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        // Act
        var first = firstScope.ServiceProvider.GetRequiredService<IPersistenceInitializer>();
        var second = secondScope.ServiceProvider.GetRequiredService<IPersistenceInitializer>();

        // Assert
        first.Should().NotBeSameAs(second);
    }

    [Fact(DisplayName = "AddPersistenceInitialization should auto-register every seeder in the assembly")]
    public void AddPersistenceInitialization_ShouldAutoRegisterEverySeeder()
    {
        // Arrange
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var expected = typeof(ApplicationDbContext).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(IPersistenceSeeder).IsAssignableFrom(type))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Act
        var registered = scope.ServiceProvider.GetServices<IPersistenceSeeder>()
            .Select(seeder => seeder.GetType().Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Assert
        expected.Should().NotBeEmpty();
        registered.Should().Equal(expected);
    }

    [Fact(DisplayName = "AddPersistenceInitialization should throw when the builder is null")]
    public void AddPersistenceInitialization_WithNullBuilder_ShouldThrow()
    {
        // Arrange
        WebApplicationBuilder? builder = null;

        // Act
        var act = () => builder!.AddPersistenceInitialization<ApplicationDbContext>();

        // Assert
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("builder");
    }

    [Fact(DisplayName = "AddPersistence should register the initializer and seeders")]
    public void AddPersistence_ShouldRegisterInitializerAndSeeders()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        services.AddLogging();
        AddBackfillDependencies(services);
        builder.AddPersistence<ApplicationDbContext>(
            "Host=127.0.0.1;Port=1;Database=qingfa;Username=qingfa;Password=qingfa;Timeout=1");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var initializer = scope.ServiceProvider.GetService<IPersistenceInitializer>();
        var steps = scope.ServiceProvider.GetServices<IInitializationStep>().ToList();

        // Assert
        initializer.Should().NotBeNull();
        steps.Should().HaveCount(6);
    }

    private static ServiceProvider BuildProvider()
    {
        WebApplicationBuilder builder = CreateBuilder();
        IServiceCollection services = builder.Services;
        builder.AddPersistenceInitialization<ApplicationDbContext>();
        return services.BuildServiceProvider();
    }

    private static void AddBackfillDependencies(IServiceCollection services)
    {
        // Initialize: the backfill seeder needs the event-store collaborators to be constructed.
        services.AddSingleton(Options.Create(new EventStoreSetting()));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IEventStore>(_ => Mock.Of<IEventStore>());
        services.AddScoped<ISnapshotStore>(_ => Mock.Of<ISnapshotStore>());
        services.AddScoped<IProjectionCheckpointStore>(_ => Mock.Of<IProjectionCheckpointStore>());
    }

    private static WebApplicationBuilder CreateBuilder()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql("Host=127.0.0.1;Port=1;Database=qingfa;Username=qingfa;Password=qingfa;Timeout=1"));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
        AddBackfillDependencies(services);
        return builder;
    }
}
