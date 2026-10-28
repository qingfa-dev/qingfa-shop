using Application.Common.Events;
using Application.Common.Persistence.UnitOfWorks;
using Application.Common.Querying.Mapping;

using Core.Common.Domain.Entities;

using Infrastructure.Events;
using Infrastructure.Persistence.EntityFramework;
using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Options;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework;

/// <summary>Test DbContext for composition tests.</summary>
public class CompositionTestDbContext : EfCoreDbContext
{
    public CompositionTestDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}

/// <summary>Simple test entity for composition tests.</summary>
public class CompositionTestProduct : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class EntityFrameworkCompositionTest
{
    #region AddEfCorePersistence with IConfiguration

    [Fact(DisplayName = "AddEfCorePersistence with config should register services")]
    public void AddEfCorePersistence_WithConfig_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["EfCore:ConnectionStringName"] = "Default",
                ["EfCore:Provider"] = "PostgreSQL"
            })
            .Build();

        // Act
        IServiceCollection result = services.AddEfCorePersistence<CompositionTestDbContext>(config);

        // Assert
        result.Should().BeSameAs(services);
        
        // Verify PersistenceSetting is registered
        using var provider = services.BuildServiceProvider();
        PersistenceSetting setting = provider.GetRequiredService<PersistenceSetting>();
        setting.ConnectionStringName.Should().Be("Default");
        setting.Provider.Should().Be("PostgreSQL");
    }

    [Fact(DisplayName = "AddEfCorePersistence should register IUnitOfWork")]
    public void AddEfCorePersistence_ShouldRegisterIUnitOfWork()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["EfCore:ConnectionStringName"] = "Default"
            })
            .Build();

        // Act
        services.AddEfCorePersistence<CompositionTestDbContext>(config);

        // Assert
        ServiceDescriptor? uowDescriptor = services
            .FirstOrDefault(d => d.ServiceType == typeof(IUnitOfWork));
        uowDescriptor.Should().NotBeNull();
        uowDescriptor!.ImplementationType.Should().Be(typeof(EfCoreUnitOfWork));
        uowDescriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact(DisplayName = "AddEfCorePersistence should register QueryMapRegistry")]
    public void AddEfCorePersistence_ShouldRegisterQueryMapRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["EfCore:ConnectionStringName"] = "Default"
            })
            .Build();

        // Act
        services.AddEfCorePersistence<CompositionTestDbContext>(config);

        // Assert
        ServiceDescriptor? registryDescriptor = services
            .FirstOrDefault(d => d.ServiceType == typeof(QueryMapRegistry));
        registryDescriptor.Should().NotBeNull();
        registryDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact(DisplayName = "AddEfCorePersistence should register QueryTranslator as open generic")]
    public void AddEfCorePersistence_ShouldRegisterQueryTranslator()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["EfCore:ConnectionStringName"] = "Default"
            })
            .Build();

        // Act
        services.AddEfCorePersistence<CompositionTestDbContext>(config);

        // Assert: registered as open generic IQueryTranslator<> -> QueryTranslationEfCore<>
        ServiceDescriptor? translatorDescriptor = services
            .FirstOrDefault(d => d.ServiceType == typeof(IQueryTranslator<>));
        translatorDescriptor.Should().NotBeNull();
        translatorDescriptor!.ImplementationType.Should().Be(typeof(QueryTranslationEfCore<>));
        translatorDescriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact(DisplayName = "AddEfCorePersistence without connection string should throw")]
    public void AddEfCorePersistence_WithoutConnectionString_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        Action act = () => services.AddEfCorePersistence<CompositionTestDbContext>(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStringName*");
    }

    [Fact(DisplayName = "AddEfCorePersistence with null services should throw")]
    public void AddEfCorePersistence_WithNullServices_ShouldThrow()
    {
        // Arrange
        IServiceCollection? services = null!;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EfCore:ConnectionString"] = "Host=localhost"
            })
            .Build();

        // Act & Assert
        Action act = () => services.AddEfCorePersistence<CompositionTestDbContext>(config);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "AddEfCorePersistence with null config should throw")]
    public void AddEfCorePersistence_WithNullConfig_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration? config = null!;

        // Act & Assert
        Action act = () => services.AddEfCorePersistence<CompositionTestDbContext>(config);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region AddEfCorePersistence with connection string

    [Fact(DisplayName = "AddEfCorePersistence with connection string should register services")]
    public void AddEfCorePersistence_WithConnectionString_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        string connectionString = "Host=localhost;Database=test;Username=test;Password=test";

        // Act
        IServiceCollection result = services.AddEfCorePersistence<CompositionTestDbContext>(connectionString);

        // Assert
        result.Should().BeSameAs(services);
        
        using var provider = services.BuildServiceProvider();
        PersistenceSetting setting = provider.GetRequiredService<PersistenceSetting>();
        setting.Provider.Should().Be("PostgreSQL");
    }

    [Theory(DisplayName = "AddEfCorePersistence with custom provider should use it")]
    [InlineData("PostgreSQL")]
    [InlineData("postgresql")]
    public void AddEfCorePersistence_WithCustomProvider_ShouldUseIt(string provider)
    {
        // Arrange
        var services = new ServiceCollection();
        string connectionString = "Host=localhost;Database=test";

        // Act
        services.AddEfCorePersistence<CompositionTestDbContext>(connectionString, provider: provider);

        // Assert
        using var sp = services.BuildServiceProvider();
        PersistenceSetting setting = sp.GetRequiredService<PersistenceSetting>();
        setting.Provider.Should().Be(provider);
    }

    [Fact(DisplayName = "AddEfCorePersistence with null connection string should throw")]
    public void AddEfCorePersistence_WithNullConnectionString_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        string? connectionString = null;

        // Act & Assert
        Action act = () => services.AddEfCorePersistence<CompositionTestDbContext>(connectionString!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "AddEfCorePersistence with empty connection string should throw")]
    public void AddEfCorePersistence_WithEmptyConnectionString_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        // ArgumentNullException.ThrowIfNullOrWhiteSpace throws ArgumentException for empty string
        Action act = () => services.AddEfCorePersistence<CompositionTestDbContext>("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "AddEfCorePersistence with whitespace connection string should throw")]
    public void AddEfCorePersistence_WithWhitespaceConnectionString_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        // ArgumentNullException.ThrowIfNullOrWhiteSpace throws ArgumentException for whitespace
        Action act = () => services.AddEfCorePersistence<CompositionTestDbContext>("   ");
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Invalid Options Validation

    [Fact(DisplayName = "AddEfCorePersistence with invalid command timeout should throw")]
    public void AddEfCorePersistence_WithInvalidCommandTimeout_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test",
                ["EfCore:ConnectionStringName"] = "Default",
                ["EfCore:Provider"] = "PostgreSQL",
                ["EfCore:CommandTimeoutSeconds"] = "0"
            })
            .Build();

        // Act & Assert
        Action act = () => services.AddEfCorePersistence<CompositionTestDbContext>(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CommandTimeout*");
    }

    [Fact(DisplayName = "AddEfCorePersistence with unsupported provider should throw")]
    public void AddEfCorePersistence_WithUnsupportedProvider_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test",
                ["EfCore:ConnectionStringName"] = "Default",
                ["EfCore:Provider"] = "SqlServer"
            })
            .Build();

        // Act & Assert
        Action act = () => services.AddEfCorePersistence<CompositionTestDbContext>(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Provider*");
    }

    [Fact(DisplayName = "AddEfCorePersistence with multiple invalid options should throw with all errors")]
    public void AddEfCorePersistence_WithMultipleInvalidOptions_ShouldThrowWithAllErrors()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test",
                ["EfCore:ConnectionStringName"] = "Default",
                ["EfCore:Provider"] = "SqlServer",
                ["EfCore:CommandTimeoutSeconds"] = "0"
            })
            .Build();

        // Act & Assert
        Action act = () => services.AddEfCorePersistence<CompositionTestDbContext>(config);
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Service Lifetimes

    [Theory(DisplayName = "AddEfCorePersistence should register services with correct lifetimes")]
    [InlineData(typeof(PersistenceSetting), ServiceLifetime.Singleton)]
    [InlineData(typeof(IUnitOfWork), ServiceLifetime.Scoped)]
    [InlineData(typeof(QueryMapRegistry), ServiceLifetime.Singleton)]
    public void AddEfCorePersistence_ShouldRegisterServicesWithCorrectLifetimes(Type serviceType, ServiceLifetime expectedLifetime)
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["EfCore:ConnectionStringName"] = "Default",
                ["EfCore:Provider"] = "PostgreSQL"
            })
            .Build();

        // Act
        services.AddEfCorePersistence<CompositionTestDbContext>(config);

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(expectedLifetime);
    }

    #endregion

    #region Domain-event publisher

    [Fact(DisplayName = "AddEfCorePersistence should register the domain-event publisher as scoped")]
    public void AddEfCorePersistence_ShouldRegisterDomainEventPublisher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEfCorePersistence<CompositionTestDbContext>("Host=localhost;Database=test");

        // Assert
        ServiceDescriptor? descriptor = services
            .FirstOrDefault(service => service.ServiceType == typeof(IDomainEventPublisher));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact(DisplayName = "Domain-event publisher should fall back to a no-op when MediatR is absent")]
    public void DomainEventPublisher_WithoutMediator_ShouldUseNullPublisher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEfCorePersistence<CompositionTestDbContext>("Host=localhost;Database=test");
        using var provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        // Act
        IDomainEventPublisher publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();

        // Assert
        publisher.Should().BeSameAs(NullDomainEventPublisher.Instance);
    }

    [Fact(DisplayName = "Domain-event publisher should use MediatR when the mediator is registered")]
    public void DomainEventPublisher_WithMediator_ShouldUseMediatRPublisher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IPublisher>());
        services.AddEfCorePersistence<CompositionTestDbContext>("Host=localhost;Database=test");
        using var provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        // Act
        IDomainEventPublisher publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();

        // Assert
        publisher.Should().BeOfType<DomainEventPublisher>();
    }

    #endregion

    #region DbContext registration

    [Fact(DisplayName = "AddEfCorePersistence should register DbContext as scoped")]
    public void AddEfCorePersistence_ShouldRegisterDbContextAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["EfCore:ConnectionStringName"] = "Default"
            })
            .Build();

        // Act
        services.AddEfCorePersistence<CompositionTestDbContext>(config);

        // Assert
        ServiceDescriptor? contextDescriptor = services
            .FirstOrDefault(d => d.ServiceType == typeof(CompositionTestDbContext));
        contextDescriptor.Should().NotBeNull();
        contextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact(DisplayName = "AddEfCorePersistence should register PersistenceSetting as singleton")]
    public void AddEfCorePersistence_ShouldRegisterPersistenceSettingAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["EfCore:ConnectionStringName"] = "Default"
            })
            .Build();

        // Act
        services.AddEfCorePersistence<CompositionTestDbContext>(config);

        // Assert
        ServiceDescriptor? settingDescriptor = services
            .FirstOrDefault(d => d.ServiceType == typeof(PersistenceSetting));
        settingDescriptor.Should().NotBeNull();
        settingDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    #endregion
}
