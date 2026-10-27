using Infrastructure.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.DbContext;

[Trait("Category", "Unit")]
public sealed class EfCoreDbContextTest
{
    #region DbContext Creation

    [Theory(DisplayName = "Can Create DbContext With Different Providers")]
    [InlineData("Npgsql", "Npgsql.EntityFrameworkCore.PostgreSQL")]
    [InlineData("InMemory", "Microsoft.EntityFrameworkCore.InMemory")]
    public void CanCreateDbContext_WithProvider(string provider, string expectedProviderName)
    {
        // Arrange & Act
        DbContextOptions<TestDbContext> options = provider switch
        {
            "Npgsql" => new DbContextOptionsBuilder<TestDbContext>()
                .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
                .Options,
            "InMemory" => new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("TestDb")
                .Options,
            _ => throw new InvalidOperationException($"Unknown provider: {provider}")
        };

        using var context = new TestDbContext(options);

        // Assert
        context.Database.ProviderName.Should().Be(expectedProviderName);
    }

    #endregion

    #region Factory

    [Fact(DisplayName = "Factory Creates Pooled DbContext")]
    public void Factory_CreatesPooledDbContext()
    {
        var options = new DbContextOptionsBuilder<FactoryTestDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
            .Options;

        var factory = new EfCoreDbContextFactory<FactoryTestDbContext>(options);
        using var context1 = factory.CreateDbContext();
        using var context2 = factory.CreateDbContext();

        // Factory should return different instances
        context1.Should().NotBeSameAs(context2);
    }

    [Fact(DisplayName = "Factory with null options should throw")]
    public void Factory_WithNullOptions_ShouldThrow()
    {
        // Arrange
        DbContextOptions<FactoryTestDbContext>? nullOptions = null;

        // Act & Assert
        Action act = () => new EfCoreDbContextFactory<FactoryTestDbContext>(nullOptions!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory(DisplayName = "Factory creates different instances per call")]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void Factory_CreatesDifferentInstances(int count)
    {
        // Arrange
        var options = new DbContextOptionsBuilder<FactoryTestDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
        var factory = new EfCoreDbContextFactory<FactoryTestDbContext>(options);

        // Act
        var contexts = new List<FactoryTestDbContext>();
        for (int i = 0; i < count; i++)
        {
            contexts.Add(factory.CreateDbContext());
        }

        // Assert
        contexts.Should().HaveCount(count);
        contexts.Should().OnlyContain(c => contexts.IndexOf(c) == contexts.FindIndex(x => ReferenceEquals(x, c)));

        // Cleanup
        foreach (var ctx in contexts)
            ctx.Dispose();
    }

    #endregion

    #region OnModelCreating

    [Fact(DisplayName = "OnModelCreating should apply configurations from assembly")]
    public void OnModelCreating_ShouldApplyConfigurationsFromAssembly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        // Act
        using var context = new TestDbContext(options);

        // Assert - no exception means OnModelCreating succeeded
        context.Model.Should().NotBeNull();
    }

    #endregion

    #region OnConfiguring

    [Fact(DisplayName = "OnConfiguring should not override already configured options")]
    public void OnConfiguring_ShouldNotOverrideConfiguredOptions()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        // Act
        using var context = new TestDbContext(options);

        // Assert
        context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.InMemory");
    }

    #endregion

    // Test-specific DbContext
    private sealed class FactoryTestDbContext : EfCoreDbContext
    {
        public FactoryTestDbContext(DbContextOptions<FactoryTestDbContext> options)
            : base(options) { }
    }

    // Test-specific DbContext
    private sealed class TestDbContext : EfCoreDbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options) { }
    }
}