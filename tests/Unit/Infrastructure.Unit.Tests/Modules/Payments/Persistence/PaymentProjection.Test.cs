using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.Messaging.Serialization;
using Application.Modules.Payments.Intents.Models;

using AwesomeAssertions;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.BuildingBlocks.Modeling.Metadata;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.Modules.Payments.Persistence;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Moq;

namespace Infrastructure.Unit.Tests.Modules.Payments.Persistence;

public sealed class PaymentProjectionTest
{
    [Fact]
    public async Task ApplyAsync_PaymentInitiated_ShouldInsertPendingRow()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var serializer = new Mock<IDomainEventSerializer>();
        var projection = new PaymentProjection(context, serializer.Object);
        Guid id = Guid.NewGuid();

        var domainEvent = new PaymentInitiated(
            id,
            PaymentProvider.Fake,
            "fake_1",
            new Money(10m, new Currency("USD")),
            PaymentCaptureMode.Automatic,
            "cart-1",
            "key-1",
            "fp-1",
            Guid.NewGuid(),
            "https://fake.pay/1",
            DateTimeOffset.UtcNow.AddMinutes(30),
            DateTimeOffset.UtcNow);

        serializer.Setup(x => x.Deserialize("PaymentInitiated", 1, It.IsAny<string>()))
            .Returns(domainEvent);

        var result = await projection.ApplyAsync(Stored("PaymentInitiated", 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        PaymentReadModel? row = await context.Set<PaymentReadModel>().FindAsync(id);
        row.Should().NotBeNull();
        row!.Status.Should().Be(PaymentStatus.Pending.ToWire());
        row.Amount.Should().Be(10m);
    }

    [Fact]
    public async Task ApplyAsync_PaymentCaptured_ShouldUpdateStatusAndAmount()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        Guid id = Guid.NewGuid();
        context.Set<PaymentReadModel>().Add(new PaymentReadModel
        {
            Id = id,
            Provider = PaymentProvider.Fake.ToWire(),
            Status = PaymentStatus.Pending.ToWire(),
            Amount = 10m,
            Currency = "USD",
            ExternalReference = "cart-1",
            IdempotencyKey = "key-1",
            IdempotencyFingerprint = "fp-1",
            CustomerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var serializer = new Mock<IDomainEventSerializer>();
        serializer.Setup(x => x.Deserialize("PaymentCaptured", 1, It.IsAny<string>()))
            .Returns(new PaymentCaptured(
                id,
                "cap_1",
                new Money(10m, new Currency("USD")),
                new Money(10m, new Currency("USD")),
                PaymentConstant.Defaults.InitialCapture,
                DateTimeOffset.UtcNow));

        var projection = new PaymentProjection(context, serializer.Object);
        var result = await projection.ApplyAsync(Stored("PaymentCaptured", 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        PaymentReadModel row = await context.Set<PaymentReadModel>().SingleAsync();
        row.Status.Should().Be(PaymentStatus.Captured.ToWire());
        row.CapturedAmount.Should().Be(10m);
        row.ProviderReference.Should().Be("cap_1");
    }

    [Fact]
    public async Task ApplyAsync_RefundSucceeded_ShouldAccumulateRefundedAmount()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        Guid paymentId = Guid.NewGuid();
        Guid refundId = Guid.NewGuid();

        context.Set<PaymentReadModel>().Add(new PaymentReadModel
        {
            Id = paymentId,
            Provider = PaymentProvider.Fake.ToWire(),
            Status = PaymentStatus.Captured.ToWire(),
            Amount = 10m,
            Currency = "USD",
            CapturedAmount = 10m,
            ExternalReference = "cart-1",
            IdempotencyKey = "key-1",
            IdempotencyFingerprint = "fp-1",
            CustomerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.Set<Application.Modules.Payments.Refunds.Models.RefundReadModel>().Add(
            new Application.Modules.Payments.Refunds.Models.RefundReadModel
            {
                Id = refundId,
                PaymentId = paymentId,
                Status = "pending",
                Amount = 10m,
                Currency = "USD",
                Reason = "returned",
                IdempotencyKey = "rk-1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var serializer = new Mock<IDomainEventSerializer>();
        serializer.Setup(x => x.Deserialize("RefundSucceeded", 1, It.IsAny<string>()))
            .Returns(new Core.Modules.Payments.Refunds.RefundSucceeded(
                refundId,
                paymentId,
                new Money(10m, new Currency("USD")),
                "re_1",
                DateTimeOffset.UtcNow));

        var projection = new PaymentProjection(context, serializer.Object);
        var result = await projection.ApplyAsync(Stored("RefundSucceeded", 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        PaymentReadModel payment = await context.Set<PaymentReadModel>().SingleAsync();
        payment.RefundedAmount.Should().Be(10m);
        payment.Status.Should().Be(PaymentStatus.Refunded.ToWire());

        var refund = await context.Set<Application.Modules.Payments.Refunds.Models.RefundReadModel>().SingleAsync();
        refund.Status.Should().Be("succeeded");
        refund.ProviderReference.Should().Be("re_1");
    }

    private static StoredEvent Stored(string eventName, long version) => new(
        StreamId: "payment-1",
        Version: version,
        EventId: Guid.NewGuid(),
        EventName: eventName,
        EventVersion: 1,
        Payload: "{}",
        Metadata: new DefaultMetadata(),
        OccurredOnUtc: DateTimeOffset.UtcNow,
        Position: version);
}
