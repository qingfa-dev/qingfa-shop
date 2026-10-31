using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Integration.Tests.BuildingBlocks.Persistence;

/// <summary>Test DbContext for EF Core integration tests.</summary>
public class TestDbContext : EfCoreDbContext
{
    public TestDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<TestProduct> Products => Set<TestProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestProduct>(entity =>
        {
            entity.ToTable("test_products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Stock).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
        });
    }
}
