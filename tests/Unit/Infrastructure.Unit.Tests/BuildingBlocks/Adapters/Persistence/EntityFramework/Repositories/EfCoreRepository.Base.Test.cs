using Application.BuildingBlocks.Persistence.Repositories;
using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Querying.Projection;
using Application.BuildingBlocks.Querying.Sorting;

using Core.BuildingBlocks.Domain.Entities;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Repositories;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.Repositories;

/// <summary>Simple test entity for unit tests — flat, no navigation properties.</summary>
public class UnitTestProduct : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

// -----------------------------------------------------------------------
// Rich entity graph for complex mapping / navigation tests
// Store (1) → (N) Category → (N) Product → (N) Review
// Product (N) ←→ (N) Tag  (many-to-many)
// Product (1) → (1) ProductDetail  (one-to-one)
// -----------------------------------------------------------------------

/// <summary>One-to-one detail entity for a product.</summary>
public class ProductDetail : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public double WeightKg { get; set; }
    public Guid ProductId { get; set; }
    public UnitTestProductV2? Product { get; set; }
}

/// <summary>Many-to-many join entity for Product-Tag.</summary>
public class ProductTag : IEntity<Guid>
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid TagId { get; set; }
    public UnitTestProductV2? Product { get; set; }
    public Tag? Tag { get; set; }
}

/// <summary>Tag entity (many-to-many with Product).</summary>
public class Tag : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ProductTag> ProductTags { get; set; } = new();
}

/// <summary>Review entity (many-to-one to Product).</summary>
public class Review : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Author { get; set; } = string.Empty;
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public Guid ProductId { get; set; }
    public UnitTestProductV2? Product { get; set; }
}

/// <summary>Category entity (one-to-many to Products).</summary>
public class Category : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public List<UnitTestProductV2> Products { get; set; } = new();
    public List<Category> SubCategories { get; set; } = new();
}

/// <summary>
/// V2 Product entity with full navigation properties:
/// - One-to-one: ProductDetail
/// - Many-to-one: Category
/// - One-to-many: Reviews
/// - Many-to-many: Tags (via ProductTag)
/// </summary>
public class UnitTestProductV2 : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool Active { get; set; }

    // Many-to-one: Category
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    // One-to-one: ProductDetail
    public Guid? ProductDetailId { get; set; }
    public ProductDetail? ProductDetail { get; set; }

    // One-to-many: Reviews
    public List<Review> Reviews { get; set; } = new();

    // Many-to-many: Tags (via ProductTag)
    public List<ProductTag> ProductTags { get; set; } = new();
}

/// <summary>Minimal DbContext for unit tests using InMemory provider.</summary>
public class UnitTestDbContext : EfCoreDbContext
{
    public UnitTestDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<UnitTestProduct> Products => Set<UnitTestProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<UnitTestProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price);
            entity.Property(e => e.Stock);
        });
    }
}

/// <summary>DbContext with rich entity graph for V2 navigation tests.</summary>
public class UnitTestDbContextV2 : EfCoreDbContext
{
    public UnitTestDbContextV2(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<UnitTestProductV2> Products => Set<UnitTestProductV2>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductDetail> ProductDetails => Set<ProductDetail>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ProductTag> ProductTags => Set<ProductTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category with self-reference
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.ParentCategory)
                .WithMany(e => e.SubCategories)
                .HasForeignKey(e => e.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Product V2
        modelBuilder.Entity<UnitTestProductV2>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price);
            entity.Property(e => e.Stock);
            entity.Property(e => e.Active);

            // Many-to-one: Category
            entity.HasOne(e => e.Category)
                .WithMany(e => e.Products)
                .HasForeignKey(e => e.CategoryId);

            // One-to-one: ProductDetail
            entity.HasOne(e => e.ProductDetail)
                .WithOne(e => e.Product)
                .HasForeignKey<ProductDetail>(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductDetail
        modelBuilder.Entity<ProductDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sku).HasMaxLength(50);
            entity.Property(e => e.WeightKg);
        });

        // Review
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Author).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Rating).IsRequired();
            entity.HasOne(e => e.Product)
                .WithMany(e => e.Reviews)
                .HasForeignKey(e => e.ProductId);
        });

        // Tag
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // ProductTag (many-to-many join)
        modelBuilder.Entity<ProductTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Product)
                .WithMany(e => e.ProductTags)
                .HasForeignKey(e => e.ProductId);
            entity.HasOne(e => e.Tag)
                .WithMany(e => e.ProductTags)
                .HasForeignKey(e => e.TagId);
        });
    }
}

/// <summary>Mock query translator that always succeeds and returns the source unchanged.</summary>
public class MockQueryTranslator<T> : IQueryTranslator<T> where T : class
{
    public Result<IQueryable<T>> ApplyFilter(IQueryable<T> source, FilterNode? filter, QueryMap<T> map)
        => Result<IQueryable<T>>.Success(source);

    public Result<IQueryable<T>> ApplySearch(IQueryable<T> source, string? search, QueryMap<T> map)
        => Result<IQueryable<T>>.Success(source);

    public Result<IQueryable<T>> ApplySort(IQueryable<T> source, IReadOnlyList<SortField> order, QueryMap<T> map)
        => Result<IQueryable<T>>.Success(source);

    public Result<IQueryable<T>> ApplyPaging(IQueryable<T> source, PageRequest page)
    {
        int skip = (page.Page - 1) * page.PageSize;
        return Result<IQueryable<T>>.Success(source.Skip(skip).Take(page.PageSize));
    }

    public IQueryable<T> IncludeNavigations(IQueryable<T> source, Query<T> query, QueryMap<T> map)
        => source;

    public Result<TabularPlan> BuildSelectPlan(
        IQueryable<T> source, IReadOnlyList<SelectField> select, QueryMap<T> map)
        => Result<TabularPlan>.Fail(
            RepositoryOutcome.Failure.QueryInvalid("Not implemented in mock"));

    public Result<TabularResult> BuildTabularClientSide(
        IReadOnlyList<T> rows, Query<T> query, QueryMap<T> map)
        => Result<TabularResult>.Fail(
            RepositoryOutcome.Failure.QueryInvalid("Not implemented in mock"));
}

/// <summary>Mock query translator that fails on specific operations for error path testing.</summary>
public class FailingMockQueryTranslator<T> : IQueryTranslator<T> where T : class
{
    public bool FailFilter { get; set; }
    public bool FailSearch { get; set; }
    public bool FailSort { get; set; }

    public Result<IQueryable<T>> ApplyFilter(IQueryable<T> source, FilterNode? filter, QueryMap<T> map)
        => FailFilter
            ? Result<IQueryable<T>>.Fail(RepositoryOutcome.Failure.QueryInvalid("Filter translation failed"))
            : Result<IQueryable<T>>.Success(source);

    public Result<IQueryable<T>> ApplySearch(IQueryable<T> source, string? search, QueryMap<T> map)
        => FailSearch
            ? Result<IQueryable<T>>.Fail(RepositoryOutcome.Failure.QueryInvalid("Search translation failed"))
            : Result<IQueryable<T>>.Success(source);

    public Result<IQueryable<T>> ApplySort(IQueryable<T> source, IReadOnlyList<SortField> order, QueryMap<T> map)
        => FailSort
            ? Result<IQueryable<T>>.Fail(RepositoryOutcome.Failure.QueryInvalid("Sort translation failed"))
            : Result<IQueryable<T>>.Success(source);

    public Result<IQueryable<T>> ApplyPaging(IQueryable<T> source, PageRequest page)
    {
        int skip = (page.Page - 1) * page.PageSize;
        return Result<IQueryable<T>>.Success(source.Skip(skip).Take(page.PageSize));
    }

    public IQueryable<T> IncludeNavigations(IQueryable<T> source, Query<T> query, QueryMap<T> map)
        => source;

    public Result<TabularPlan> BuildSelectPlan(
        IQueryable<T> source, IReadOnlyList<SelectField> select, QueryMap<T> map)
        => Result<TabularPlan>.Fail(
            RepositoryOutcome.Failure.QueryInvalid("Not implemented in mock"));

    public Result<TabularResult> BuildTabularClientSide(
        IReadOnlyList<T> rows, Query<T> query, QueryMap<T> map)
        => Result<TabularResult>.Fail(
            RepositoryOutcome.Failure.QueryInvalid("Not implemented in mock"));
}

/// <summary>Mock query translator for tabular projection tests.</summary>
public class TabularMockQueryTranslator<T> : IQueryTranslator<T> where T : class
{
    public Result<IQueryable<T>> ApplyFilter(IQueryable<T> source, FilterNode? filter, QueryMap<T> map)
        => Result<IQueryable<T>>.Success(source);

    public Result<IQueryable<T>> ApplySearch(IQueryable<T> source, string? search, QueryMap<T> map)
        => Result<IQueryable<T>>.Success(source);

    public Result<IQueryable<T>> ApplySort(IQueryable<T> source, IReadOnlyList<SortField> order, QueryMap<T> map)
        => Result<IQueryable<T>>.Success(source);

    public Result<IQueryable<T>> ApplyPaging(IQueryable<T> source, PageRequest page)
    {
        int skip = (page.Page - 1) * page.PageSize;
        return Result<IQueryable<T>>.Success(source.Skip(skip).Take(page.PageSize));
    }

    public IQueryable<T> IncludeNavigations(IQueryable<T> source, Query<T> query, QueryMap<T> map)
        => source;

    public Result<TabularPlan> BuildSelectPlan(
        IQueryable<T> source, IReadOnlyList<SelectField> select, QueryMap<T> map)
    {
        // Build a simple select plan for testing using SelectField constructor params
        var columns = select.Select(s => new TabularColumn(s.Field, typeof(string))).ToList();
        // Materialize to a list first, then project — keeps the result as IQueryable for paging
        var materialized = source.ToList();
        IEnumerable<object[]> rows = materialized.Select(e =>
        {
            object[] row = new object[select.Count];
            for (int i = 0; i < select.Count; i++)
            {
                var prop = e.GetType().GetProperty(select[i].Field);
                row[i] = prop?.GetValue(e) ?? System.DBNull.Value;
            }
            return row;
        });
        IQueryable<object[]> rowsQuery = rows.AsQueryable();
        return Result<TabularPlan>.Success(new TabularPlan(rowsQuery, columns));
    }

    public Result<TabularResult> BuildTabularClientSide(
        IReadOnlyList<T> rows, Query<T> query, QueryMap<T> map)
    {
        var tabularRows = rows.Select(r => new TabularRow(new List<object?>
        {
            r.GetType().GetProperty("Name")?.GetValue(r) ?? null,
        })).ToList();
        var columns = new List<TabularColumn> { new TabularColumn("Name", typeof(string)) };
        return Result<TabularResult>.Success(new TabularResult(columns, tabularRows));
    }
}

/// <summary>Shared test fixture for EfCoreRepository tests.</summary>
public static class EfCoreRepositoryTestFixture
{
    public static UnitTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new UnitTestDbContext(options);
    }

    public static QueryMap<UnitTestProduct> CreateQueryMap()
    {
        var result = QueryMapBuilder<UnitTestProduct>.Create()
            .Map("Name", p => p.Name, b => b.Searchable().Sortable())
            .Map("Price", p => p.Price, b => b.Sortable())
            .Map("Stock", p => p.Stock, b => b.Sortable())
            .Build();
        return result.Value!;
    }

    public static IQueryMapProvider CreateMapProvider<T>(QueryMap<T> map) where T : class
    {
        var registry = new QueryMapRegistry();
        registry.Register(map);
        return registry;
    }

    public static EfCoreRepository<UnitTestProduct, Guid> CreateRepository(UnitTestDbContext context)
    {
        var translator = new MockQueryTranslator<UnitTestProduct>();
        var provider = CreateMapProvider(CreateQueryMap());
        var capabilities = new Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Capabilities.EfCoreQueryCapabilities();
        return new EfCoreRepository<UnitTestProduct, Guid>(context, translator, provider, capabilities);
    }

    public static EfCoreRepository<UnitTestProduct, Guid> CreateRepositoryWithTranslator(
        UnitTestDbContext context, IQueryTranslator<UnitTestProduct> translator)
    {
        var provider = CreateMapProvider(CreateQueryMap());
        var capabilities = new Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Capabilities.EfCoreQueryCapabilities();
        return new EfCoreRepository<UnitTestProduct, Guid>(context, translator, provider, capabilities);
    }

    public static UnitTestProduct CreateProduct(string name, decimal price) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Price = price,
        Stock = 100
    };

    public static async Task SeedProductsAsync(UnitTestDbContext context, int count)
    {
        var products = Enumerable.Range(1, count)
            .Select(i => CreateProduct($"Product {i}", i * 10m))
            .ToList();
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    // -------------------------------------------------------------------
    // V2 Context and Repository helpers
    // -------------------------------------------------------------------

    public static UnitTestDbContextV2 CreateContextV2()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new UnitTestDbContextV2(options);
    }

    public static QueryMap<UnitTestProductV2> CreateQueryMapV2()
    {
        var result = QueryMapBuilder<UnitTestProductV2>.Create()
            .Map("Name", p => p.Name, b => b.Searchable().Sortable())
            .Map("Price", p => p.Price, b => b.Sortable())
            .Map("Stock", p => p.Stock, b => b.Sortable())
            .Map("Active", p => p.Active, b => b.Sortable())
            .Build();
        return result.Value!;
    }

    public static EfCoreRepository<UnitTestProductV2, Guid> CreateRepositoryV2(UnitTestDbContextV2 context)
    {
        var translator = new MockQueryTranslator<UnitTestProductV2>();
        var provider = CreateMapProvider(CreateQueryMapV2());
        var capabilities = new Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Capabilities.EfCoreQueryCapabilities();
        return new EfCoreRepository<UnitTestProductV2, Guid>(context, translator, provider, capabilities);
    }

    public static EfCoreRepository<UnitTestProductV2, Guid> CreateRepositoryV2WithTranslator(
        UnitTestDbContextV2 context, IQueryTranslator<UnitTestProductV2> translator)
    {
        var provider = CreateMapProvider(CreateQueryMapV2());
        var capabilities = new Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Capabilities.EfCoreQueryCapabilities();
        return new EfCoreRepository<UnitTestProductV2, Guid>(context, translator, provider, capabilities);
    }

    /// <summary>Creates a product with all navigation properties populated.</summary>
    public static UnitTestProductV2 CreateProductV2(
        string name,
        decimal price,
        int stock = 100,
        bool active = true,
        string? categoryName = null,
        string? detailSku = null,
        int reviewCount = 0,
        string[]? tagNames = null)
    {
        var categoryId = Guid.NewGuid();
        var product = new UnitTestProductV2
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            Stock = stock,
            Active = active,
            CategoryId = categoryId,
            Category = new Category { Id = categoryId, Name = categoryName ?? "General" }
        };

        if (detailSku != null)
        {
            var detailId = Guid.NewGuid();
            product.ProductDetailId = detailId;
            product.ProductDetail = new ProductDetail
            {
                Id = detailId,
                ProductId = product.Id,
                Sku = detailSku,
                Description = $"Description for {name}",
                WeightKg = 1.5
            };
        }

        for (int i = 0; i < reviewCount; i++)
        {
            product.Reviews.Add(new Review
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Author = $"Reviewer {i + 1}",
                Rating = (i % 5) + 1,
                Comment = $"Review {i + 1} comment"
            });
        }

        if (tagNames != null)
        {
            foreach (var tagName in tagNames)
            {
                var tagId = Guid.NewGuid();
                product.ProductTags.Add(new ProductTag
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    TagId = tagId,
                    Tag = new Tag { Id = tagId, Name = tagName }
                });
            }
        }

        return product;
    }

    /// <summary>Seeds a rich product graph with categories, details, reviews, and tags.</summary>
    public static async Task SeedRichProductsAsync(UnitTestDbContextV2 context, int count)
    {
        // Create categories
        var electronics = new Category { Id = Guid.NewGuid(), Name = "Electronics" };
        var books = new Category { Id = Guid.NewGuid(), Name = "Books" };
        var electronicsAccessories = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Accessories",
            ParentCategoryId = electronics.Id
        };
        context.Categories.AddRange(electronics, books, electronicsAccessories);

        // Create tags
        var tags = new[] { "New", "Sale", "Popular", "Featured" }.Select(n => new Tag { Id = Guid.NewGuid(), Name = n }).ToList();
        context.Tags.AddRange(tags);

        // Create products with varying navigation data
        var products = new List<UnitTestProductV2>();
        for (int i = 1; i <= count; i++)
        {
            var category = i % 3 == 0 ? books : (i % 2 == 0 ? electronicsAccessories : electronics);
            var reviewCount = i % 3; // 0, 1, or 2 reviews
            var tagCount = i % 4; // 0-3 tags
            var selectedTags = tags.Take(tagCount).Select(t => t.Name).ToArray();

            var product = CreateProductV2(
                name: $"Product V2 {i}",
                price: i * 15.5m,
                stock: i * 10,
                active: i % 5 != 0, // every 5th product is inactive
                categoryName: category.Name,
                detailSku: i % 2 == 0 ? $"SKU-{i:000}" : null, // even products have details
                reviewCount: reviewCount,
                tagNames: selectedTags.Length > 0 ? selectedTags : null
            );
            product.CategoryId = category.Id;
            product.Category = category;
            products.Add(product);
        }

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}
