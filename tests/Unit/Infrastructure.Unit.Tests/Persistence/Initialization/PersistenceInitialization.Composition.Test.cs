using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Seeders;
using Application.Common.Persistence.UnitOfWorks;

using Core.Todos.Categories;
using Core.Todos.Items;
using Core.Todos.Lists;

using Infrastructure.Persistence;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;
using Infrastructure.Persistence.Initialization;
using Infrastructure.Security.Identity.Domain.Roles;
using Infrastructure.Security.Identity.Domain.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        steps.Should().HaveCount(5);
        steps.Select(step => step.Name).Should().BeEquivalentTo(
            "seeder:identity-roles",
            "seeder:identity-users",
            "seeder:todo-categories",
            "seeder:todo-lists",
            "seeder:todos");
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
        seeders.Should().HaveCount(5);
        seeders.Select(seeder => seeder.EntityType).Should().BeEquivalentTo(
            new[]
            {
                typeof(Role),
                typeof(User),
                typeof(TodoCategory),
                typeof(TodoList),
                typeof(Todo)
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
        var services = CreateServices();
        services.AddPersistenceInitialization<ApplicationDbContext>();
        services.AddPersistenceInitialization<ApplicationDbContext>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var steps = scope.ServiceProvider.GetServices<IInitializationStep>().ToList();

        // Assert
        steps.Should().HaveCount(5);
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

    [Fact(DisplayName = "AddPersistenceInitialization should throw when services is null")]
    public void AddPersistenceInitialization_WithNullServices_ShouldThrow()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddPersistenceInitialization<ApplicationDbContext>();

        // Assert
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("services");
    }

    [Fact(DisplayName = "AddPersistence should register the initializer and seeders")]
    public void AddPersistence_ShouldRegisterInitializerAndSeeders()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPersistence<ApplicationDbContext>(
            "Host=127.0.0.1;Port=1;Database=qingfa;Username=qingfa;Password=qingfa;Timeout=1");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var initializer = scope.ServiceProvider.GetService<IPersistenceInitializer>();
        var steps = scope.ServiceProvider.GetServices<IInitializationStep>().ToList();

        // Assert
        initializer.Should().NotBeNull();
        steps.Should().HaveCount(5);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = CreateServices();
        services.AddPersistenceInitialization<ApplicationDbContext>();
        return services.BuildServiceProvider();
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql("Host=127.0.0.1;Port=1;Database=qingfa;Username=qingfa;Password=qingfa;Timeout=1"));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
        return services;
    }
}
