using Application.BuildingBlocks.Querying.Pagination;
using Application.Modules.Payments.Intents.Models;

using AwesomeAssertions;

using Core.Modules.Payments;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.Modules.Payments.Persistence;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Modules.Payments.Persistence;

public sealed class PaymentReadStoreTest
{
    [Fact]
    public async Task FindAsync_ShouldReturnTheRow()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        PaymentReadModel row = await SeedAsync(context, "cart-1", 10m, "pending");

        var store = new PaymentReadStore(context);

        (await store.FindAsync(row.Id, CancellationToken.None))!.Id.Should().Be(row.Id);
    }

    [Fact]
    public async Task FindByProviderReferenceAsync_ShouldMatchProviderAndReference()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        PaymentReadModel row = await SeedAsync(context, "cart-2", 10m, "pending");

        var store = new PaymentReadStore(context);

        PaymentReadModel? found = await store.FindByProviderReferenceAsync(
            PaymentProvider.Fake, row.ProviderReference!, CancellationToken.None);

        found!.Id.Should().Be(row.Id);
    }

    [Fact]
    public async Task FindByIdempotencyKeyAsync_ShouldScopeToTheCustomer()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        PaymentReadModel row = await SeedAsync(context, "cart-3", 10m, "pending");

        var store = new PaymentReadStore(context);

        (await store.FindByIdempotencyKeyAsync(row.CustomerId, row.IdempotencyKey, CancellationToken.None))!
            .Id.Should().Be(row.Id);

        (await store.FindByIdempotencyKeyAsync(Guid.NewGuid(), row.IdempotencyKey, CancellationToken.None))
            .Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_AndCountAsync_ShouldFilterByExternalReference()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        PaymentReadModel first = await SeedAsync(context, "cart-a", 10m, "pending");
        await SeedAsync(context, "cart-b", 20m, "captured", first.CustomerId);

        var store = new PaymentReadStore(context);

        IReadOnlyList<PaymentReadModel> rows = await store.ListAsync(
            first.CustomerId, "cart-a", 0, 10, CancellationToken.None);

        rows.Should().ContainSingle();
        (await store.CountAsync(first.CustomerId, null, CancellationToken.None)).Should().Be(2);
    }

    [Fact]
    public async Task SearchAsync_ShouldApplyEveryFilter()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        PaymentReadModel row = await SeedAsync(context, "cart-search", 15m, "captured");

        var store = new PaymentReadStore(context);

        QueryPage<PaymentReadModel> page = await store.SearchAsync(
            new PaymentSearchFilter(
                "captured",
                "fake",
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(1)),
            new PageRequest(1, 10),
            CancellationToken.None);

        page.TotalCount.Should().Be(1);
        page.Items.Single().Id.Should().Be(row.Id);
    }

    [Fact]
    public async Task SearchAsync_WithNoMatches_ShouldReturnAnEmptyPage()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        await SeedAsync(context, "cart-none", 15m, "captured");

        var store = new PaymentReadStore(context);

        QueryPage<PaymentReadModel> page = await store.SearchAsync(
            new PaymentSearchFilter("failed", "stripe", null, null),
            new PageRequest(1, 10),
            CancellationToken.None);

        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task FindRefundAsync_ShouldReturnTheRefund()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        PaymentReadModel row = await SeedAsync(context, "cart-refund", 15m, "captured");

        var refund = new Application.Modules.Payments.Refunds.Models.RefundReadModel
        {
            Id = Guid.NewGuid(),
            PaymentId = row.Id,
            Status = "pending",
            Amount = 5m,
            Currency = "USD",
            Reason = "returned",
            IdempotencyKey = "rk-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Set<Application.Modules.Payments.Refunds.Models.RefundReadModel>().Add(refund);
        await context.SaveChangesAsync();

        var store = new PaymentReadStore(context);

        RefundFound(await store.FindRefundAsync(row.Id, refund.Id, CancellationToken.None), refund.Id);
        (await store.FindRefundAsync(row.Id, Guid.NewGuid(), CancellationToken.None)).Should().BeNull();
    }

    /// <summary>Asserts a refund lookup result.</summary>
    private static void RefundFound(
        Application.Modules.Payments.Refunds.Models.RefundReadModel? found,
        Guid expected)
    {
        found.Should().NotBeNull();
        found!.Id.Should().Be(expected);
    }

    /// <summary>Seeds one projected payment.</summary>
    private static async Task<PaymentReadModel> SeedAsync(
        ApplicationDbContext context,
        string externalReference,
        decimal amount,
        string status,
        Guid? customerId = null)
    {
        var row = new PaymentReadModel
        {
            Id = Guid.NewGuid(),
            Provider = PaymentProvider.Fake.ToWire(),
            ProviderReference = $"fake_{Guid.NewGuid():N}",
            Status = status,
            Amount = amount,
            Currency = "USD",
            ExternalReference = externalReference,
            IdempotencyKey = Guid.NewGuid().ToString(),
            IdempotencyFingerprint = "fp",
            CustomerId = customerId ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Set<PaymentReadModel>().Add(row);
        await context.SaveChangesAsync();

        return row;
    }

    /// <summary>Creates an in-memory database with the module's mappings applied.</summary>
    private static async Task<ApplicationDbContext> NewContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return context;
    }
}
