using Application.Modules.Payments.Persistence;

using AwesomeAssertions;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.Modules.Payments.Persistence;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Modules.Payments.Persistence;

public sealed class PaymentIdempotencyStoreTest
{
    [Fact]
    public async Task ReserveAsync_SameKeyTwice_ShouldReturnExisting()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        var store = new PaymentIdempotencyStore(context, TimeProvider.System);
        Guid customerId = Guid.NewGuid();

        Result<IdempotencyReservation> first = await store.ReserveAsync(
            customerId, "key-1", "fp-1", CancellationToken.None);

        Result<IdempotencyReservation> second = await store.ReserveAsync(
            customerId, "key-1", "fp-1", CancellationToken.None);

        first.Value.IsNew.Should().BeTrue();
        second.Value.IsNew.Should().BeFalse();
        second.Value.PaymentId.Should().Be(first.Value.PaymentId);
        second.Value.FingerprintMatches.Should().BeTrue();
    }

    [Fact]
    public async Task ReserveAsync_SameKeyDifferentFingerprint_ShouldReportMismatch()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        var store = new PaymentIdempotencyStore(context, TimeProvider.System);
        Guid customerId = Guid.NewGuid();

        await store.ReserveAsync(customerId, "key-1", "fp-1", CancellationToken.None);

        Result<IdempotencyReservation> second = await store.ReserveAsync(
            customerId, "key-1", "fp-2", CancellationToken.None);

        second.Value.IsNew.Should().BeFalse();
        second.Value.FingerprintMatches.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldAllowAFreshReservation()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        var store = new PaymentIdempotencyStore(context, TimeProvider.System);
        Guid customerId = Guid.NewGuid();

        Result<IdempotencyReservation> first = await store.ReserveAsync(
            customerId, "key-1", "fp-1", CancellationToken.None);

        await store.ReleaseAsync(customerId, "key-1", first.Value.PaymentId, CancellationToken.None);

        Result<IdempotencyReservation> second = await store.ReserveAsync(
            customerId, "key-1", "fp-1", CancellationToken.None);

        second.Value.IsNew.Should().BeTrue();
        second.Value.PaymentId.Should().NotBe(first.Value.PaymentId);
    }

    [Fact]
    public async Task ReleaseAsync_UnknownReservation_ShouldSucceed()
    {
        await using ApplicationDbContext context = await NewContextAsync();
        var store = new PaymentIdempotencyStore(context, TimeProvider.System);

        Result result = await store.ReleaseAsync(
            Guid.NewGuid(), "missing", Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
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
