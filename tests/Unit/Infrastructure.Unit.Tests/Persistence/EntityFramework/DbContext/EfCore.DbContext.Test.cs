using Infrastructure.Persistence.EntityFramework.DbContext;
using Infrastructure.Persistence.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.EntityFramework.DbContext;

[Trait("Category", "Unit")]
public sealed class EfCoreDbContextTest
{
    [Fact(DisplayName = "Can Create DbContext With PostgreSQL Provider")]
    public void CanCreateDbContext_WithPostgreSQL()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
            .Options;

        using var context = new TestDbContext(options);
        context.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Fact(DisplayName = "Can Create DbContext With SQL Server Provider")]
    public void CanCreateDbContext_WithSqlServer()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer("Server=localhost;Database=test;User=test;Password=test;TrustServerCertificate=True")
            .Options;

        using var context = new TestDbContext(options);
        context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");
    }

    [Fact(DisplayName = "Factory Creates Pooled DbContext")]
    public void Factory_CreatesPooledDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
            .Options;

        var factory = new EfCoreDbContextFactory<TestDbContext>(options);
        using var context1 = factory.CreateDbContext();
        using var context2 = factory.CreateDbContext();

        // Factory should return different instances
        context1.Should().NotBeSameAs(context2);
    }

    // Test-specific DbContext
    private sealed class TestDbContext : EfCoreDbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    }
}