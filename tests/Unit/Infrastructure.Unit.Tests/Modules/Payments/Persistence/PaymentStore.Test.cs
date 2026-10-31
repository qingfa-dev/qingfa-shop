using AwesomeAssertions;

using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.Modules.Payments.Persistence;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Modules.Payments.Persistence;

public sealed class PaymentStoreTest
{
    [Fact]
    public async Task WebhookStore_FirstDelivery_ShouldRecord()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        var store = new WebhookEventStore(context, TimeProvider.System);

        Result<bool> result = await store.TryRecordAsync(
            "fake", "evt_1", "Captured", "{}", CancellationToken.None);

        result.Value.Should().BeTrue();
        (await context.Set<PaymentWebhookEvent>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task WebhookStore_DuplicateDelivery_ShouldNotRecordTwice()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        var store = new WebhookEventStore(context, TimeProvider.System);

        await store.TryRecordAsync("fake", "evt_1", "Captured", "{}", CancellationToken.None);
        Result<bool> second = await store.TryRecordAsync(
            "fake", "evt_1", "Captured", "{}", CancellationToken.None);

        second.Value.Should().BeFalse();
        (await context.Set<PaymentWebhookEvent>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task WebhookStore_MarkProcessed_ShouldStoreTheOutcome()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        var store = new WebhookEventStore(context, TimeProvider.System);

        await store.TryRecordAsync("fake", "evt_1", "Captured", "{}", CancellationToken.None);
        await store.MarkProcessedAsync(
            "fake", "evt_1", PaymentConstant.Outcomes.Settled, CancellationToken.None);

        PaymentWebhookEvent row = await context.Set<PaymentWebhookEvent>().SingleAsync();
        row.Outcome.Should().Be(PaymentConstant.Outcomes.Settled);
        row.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task WebhookStore_MarkUnknownDelivery_ShouldSucceed()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        var store = new WebhookEventStore(context, TimeProvider.System);

        Result result = await store.MarkProcessedAsync(
            "fake", "missing", PaymentConstant.Outcomes.Settled, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReferenceStore_RecordThenFind_ShouldRoundTrip()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        var store = new PaymentReferenceStore(context, TimeProvider.System);
        Guid paymentId = Guid.NewGuid();

        await store.RecordAsync(PaymentProvider.Fake, "fake_1", paymentId, CancellationToken.None);

        (await store.FindAsync(PaymentProvider.Fake, "fake_1", CancellationToken.None))
            .Should().Be(paymentId);
    }

    [Fact]
    public async Task ReferenceStore_RecordTwice_ShouldKeepOneRow()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        var store = new PaymentReferenceStore(context, TimeProvider.System);
        Guid paymentId = Guid.NewGuid();

        await store.RecordAsync(PaymentProvider.Fake, "fake_1", paymentId, CancellationToken.None);
        await store.RecordAsync(PaymentProvider.Fake, "fake_1", paymentId, CancellationToken.None);

        (await context.Set<PaymentReference>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ReferenceStore_UnknownReference_ShouldReturnNull()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        var store = new PaymentReferenceStore(context, TimeProvider.System);

        (await store.FindAsync(PaymentProvider.Stripe, "missing", CancellationToken.None))
            .Should().BeNull();
    }

    [Fact]
    public async Task WebhookStore_DeleteProcessedBefore_ShouldPruneOnlySettledOldRows()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        var store = new WebhookEventStore(context, TimeProvider.System);

        DateTime now = DateTime.UtcNow;
        DateTime old = now.AddDays(-40);

        context.Set<PaymentWebhookEvent>().AddRange(
            new PaymentWebhookEvent
            {
                Id = Guid.NewGuid(),
                Provider = "fake",
                ProviderEventId = "old-settled",
                EventType = "Captured",
                RawBody = "{}",
                Outcome = PaymentConstant.Outcomes.Settled,
                ReceivedAt = old,
                ProcessedAt = old
            },
            new PaymentWebhookEvent
            {
                Id = Guid.NewGuid(),
                Provider = "fake",
                ProviderEventId = "old-unprocessed",
                EventType = "Captured",
                RawBody = "{}",
                Outcome = PaymentConstant.Outcomes.Received,
                ReceivedAt = old
            },
            new PaymentWebhookEvent
            {
                Id = Guid.NewGuid(),
                Provider = "fake",
                ProviderEventId = "recent-settled",
                EventType = "Captured",
                RawBody = "{}",
                Outcome = PaymentConstant.Outcomes.Settled,
                ReceivedAt = now,
                ProcessedAt = now
            });

        await context.SaveChangesAsync();

        Result<int> deleted = await store.DeleteProcessedBeforeAsync(
            now.AddDays(-30), CancellationToken.None);

        deleted.Value.Should().Be(1);

        List<string> remaining = await context.Set<PaymentWebhookEvent>()
            .Select(row => row.ProviderEventId)
            .ToListAsync();

        remaining.Should().BeEquivalentTo(["old-unprocessed", "recent-settled"]);
    }

    [Fact]
    public async Task WebhookStore_DeleteProcessedBefore_WhenNothingMatches_ShouldReturnZero()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        var store = new WebhookEventStore(context, TimeProvider.System);

        Result<int> deleted = await store.DeleteProcessedBeforeAsync(
            DateTimeOffset.UtcNow, CancellationToken.None);

        deleted.Value.Should().Be(0);
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