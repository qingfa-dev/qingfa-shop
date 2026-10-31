using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Intents.Queries;
using Application.Modules.Payments.Persistence;

using Application.Unit.Tests.Modules.Payments.Fixtures;

using AwesomeAssertions;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Moq;

namespace Application.Unit.Tests.Modules.Payments.Queries;

public sealed class GetPaymentTest
{
    [Fact]
    public async Task Handle_OtherCustomersPayment_ShouldReturnNotFound()
    {
        var row = new PaymentReadModel
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Status = "pending",
            Currency = PaymentConstant.Defaults.Currency
        };

        var store = new Mock<IPaymentReadStore>();
        store.Setup(x => x.FindAsync(row.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentReadModel>.Success(row));

        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        var handler = new GetPayment.UseCaseHandler(store.Object, repository.Object, new StubPaymentPolicy(), TimeProvider.System);
        var query = new GetPayment.UseCase(row.Id, Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.NotFound);
    }

    [Fact]
    public async Task Handle_AdminRead_ShouldIgnoreOwnership()
    {
        var row = new PaymentReadModel
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Status = "pending",
            Currency = PaymentConstant.Defaults.Currency
        };

        var store = new Mock<IPaymentReadStore>();
        store.Setup(x => x.FindAsync(row.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentReadModel>.Success(row));

        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        var handler = new GetPayment.UseCaseHandler(store.Object, repository.Object, new StubPaymentPolicy(), TimeProvider.System);
        var query = new GetPayment.UseCase(row.Id, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(row.Id);
    }

    [Fact]
    public async Task Handle_ProjectionMiss_ShouldFallBackToTheEventStream()
    {
        Payment payment = PaymentFactory.Initiate(
            Guid.NewGuid(),
            PaymentProvider.Fake,
            "fake_1",
            new Money(10m, new Currency("USD")),
            PaymentCaptureMode.Automatic,
            "cart-1",
            "key-1",
            "fp-1",
            Guid.NewGuid(),
            null,
            null,
            DateTimeOffset.UtcNow).Value;

        var store = new Mock<IPaymentReadStore>();
        store.Setup(x => x.FindAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentReadModel>.Fail(PaymentsOutcome.Failure.NotFound(payment.Id)));

        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        repository.Setup(x => x.LoadAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Payment>.Success(payment));

        var handler = new GetPayment.UseCaseHandler(store.Object, repository.Object, new StubPaymentPolicy(), TimeProvider.System);

        var result = await handler.Handle(
            new GetPayment.UseCase(payment.Id, payment.CustomerId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(payment.Id);
    }

    [Fact]
    public async Task Handle_ProjectionMissAndUnknownPayment_ShouldReturnNotFound()
    {
        Guid id = Guid.NewGuid();

        var store = new Mock<IPaymentReadStore>();
        store.Setup(x => x.FindAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentReadModel>.Fail(PaymentsOutcome.Failure.NotFound(id)));

        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        repository.Setup(x => x.LoadAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Payment>.Fail(PaymentsOutcome.Failure.NotFound(id)));

        var handler = new GetPayment.UseCaseHandler(store.Object, repository.Object, new StubPaymentPolicy(), TimeProvider.System);

        var result = await handler.Handle(new GetPayment.UseCase(id, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.NotFound);
    }
}
