using Application.BuildingBlocks.Messaging.Events;
using Application.BuildingBlocks.Persistence.UnitOfWorks;
using Application.BuildingBlocks.Querying.Mapping;

using Core.BuildingBlocks.Domain.Entities;

using Infrastructure.BuildingBlocks.Adapters.Events;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Options;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.UnitOfWorks;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework;

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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["EfCore:ConnectionStringName"] = "Default",
                ["EfCore:Provider"] = "PostgreSQL"
            })
            .Build();

        // Act
        WebApplicationBuilder result = builder.AddEfCorePersistence<CompositionTestDbContext>(config);

        // Assert
        result.Should().BeSameAs(builder);
        
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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["EfCore:ConnectionStringName"] = "Default"
            })
            .Build();

        // Act
        builder.AddEfCorePersistence<CompositionTestDbContext>(config);

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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["EfCore:ConnectionStringName"] = "Default"
            })
            .Build();

        // Act
        builder.AddEfCorePersistence<CompositionTestDbContext>(config);

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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["EfCore:ConnectionStringName"] = "Default"
            })
            .Build();

        // Act
        builder.AddEfCorePersistence<CompositionTestDbContext>(config);

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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        Action act = () => builder.AddEfCorePersistence<CompositionTestDbContext>(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStringName*");
    }

    [Fact(DisplayName = "AddEfCorePersistence with null builder should throw")]
    public void AddEfCorePersistence_WithNullBuilder_ShouldThrow()
    {
        // Arrange
        WebApplicationBuilder? builder = null;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EfCore:ConnectionString"] = "Host=localhost"
            })
            .Build();

        // Act & Assert
        Action act = () => builder!.AddEfCorePersistence<CompositionTestDbContext>(config);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "AddEfCorePersistence with null config should throw")]
    public void AddEfCorePersistence_WithNullConfig_ShouldThrow()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        IConfiguration? config = null!;

        // Act & Assert
        Action act = () => builder.AddEfCorePersistence<CompositionTestDbContext>(config);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region AddEfCorePersistence with connection string

    [Fact(DisplayName = "AddEfCorePersistence with connection string should register services")]
    public void AddEfCorePersistence_WithConnectionString_ShouldRegisterServices()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        string connectionString = "Host=localhost;Database=test;Username=test;Password=test";

        // Act
        WebApplicationBuilder result = builder.AddEfCorePersistence<CompositionTestDbContext>(connectionString);

        // Assert
        result.Should().BeSameAs(builder);
        
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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        string connectionString = "Host=localhost;Database=test";

        // Act
        builder.AddEfCorePersistence<CompositionTestDbContext>(connectionString, provider: provider);

        // Assert
        using var sp = services.BuildServiceProvider();
        PersistenceSetting setting = sp.GetRequiredService<PersistenceSetting>();
        setting.Provider.Should().Be(provider);
    }

    [Fact(DisplayName = "AddEfCorePersistence with null connection string should throw")]
    public void AddEfCorePersistence_WithNullConnectionString_ShouldThrow()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        string? connectionString = null;

        // Act & Assert
        Action act = () => builder.AddEfCorePersistence<CompositionTestDbContext>(connectionString!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "AddEfCorePersistence with empty connection string should throw")]
    public void AddEfCorePersistence_WithEmptyConnectionString_ShouldThrow()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;

        // Act & Assert
        // ArgumentNullException.ThrowIfNullOrWhiteSpace throws ArgumentException for empty string
        Action act = () => builder.AddEfCorePersistence<CompositionTestDbContext>("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "AddEfCorePersistence with whitespace connection string should throw")]
    public void AddEfCorePersistence_WithWhitespaceConnectionString_ShouldThrow()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;

        // Act & Assert
        // ArgumentNullException.ThrowIfNullOrWhiteSpace throws ArgumentException for whitespace
        Action act = () => builder.AddEfCorePersistence<CompositionTestDbContext>("   ");
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Invalid Options Validation

    [Fact(DisplayName = "AddEfCorePersistence with invalid command timeout should throw")]
    public void AddEfCorePersistence_WithInvalidCommandTimeout_ShouldThrow()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
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
        Action act = () => builder.AddEfCorePersistence<CompositionTestDbContext>(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CommandTimeout*");
    }

    [Fact(DisplayName = "AddEfCorePersistence with unsupported provider should throw")]
    public void AddEfCorePersistence_WithUnsupportedProvider_ShouldThrow()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test",
                ["EfCore:ConnectionStringName"] = "Default",
                ["EfCore:Provider"] = "SqlServer"
            })
            .Build();

        // Act & Assert
        Action act = () => builder.AddEfCorePersistence<CompositionTestDbContext>(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Provider*");
    }

    [Fact(DisplayName = "AddEfCorePersistence with multiple invalid options should throw with all errors")]
    public void AddEfCorePersistence_WithMultipleInvalidOptions_ShouldThrowWithAllErrors()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
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
        Action act = () => builder.AddEfCorePersistence<CompositionTestDbContext>(config);
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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["EfCore:ConnectionStringName"] = "Default",
                ["EfCore:Provider"] = "PostgreSQL"
            })
            .Build();

        // Act
        builder.AddEfCorePersistence<CompositionTestDbContext>(config);

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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;

        // Act
        builder.AddEfCorePersistence<CompositionTestDbContext>("Host=localhost;Database=test");

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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        builder.AddEfCorePersistence<CompositionTestDbContext>("Host=localhost;Database=test");
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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        services.AddSingleton(Mock.Of<IPublisher>());
        builder.AddEfCorePersistence<CompositionTestDbContext>("Host=localhost;Database=test");
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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["EfCore:ConnectionStringName"] = "Default"
            })
            .Build();

        // Act
        builder.AddEfCorePersistence<CompositionTestDbContext>(config);

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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["EfCore:ConnectionStringName"] = "Default"
            })
            .Build();

        // Act
        builder.AddEfCorePersistence<CompositionTestDbContext>(config);

        // Assert
        ServiceDescriptor? settingDescriptor = services
            .FirstOrDefault(d => d.ServiceType == typeof(PersistenceSetting));
        settingDescriptor.Should().NotBeNull();
        settingDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    #endregion
}
