using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Intents.Commands;
using Application.Modules.Payments.Persistence;
using Application.Modules.Payments.Ports.Models;

using AwesomeAssertions;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Moq;

namespace Application.Unit.Tests.Modules.Payments.Commands;

public sealed class SettlePaymentTest
{
    [Fact]
    public async Task Handle_AlreadyCaptured_ShouldBeANoOp()
    {
        Payment payment = PaymentFactory.Initiate(
            Guid.NewGuid(), PaymentProvider.Fake, "fake_1", new Money(10m, new Currency("USD")),
            PaymentCaptureMode.Automatic, "cart-1", "key-1", "fp-1", Guid.NewGuid(),
            null, null, DateTimeOffset.UtcNow).Value;

        payment.MarkProcessing("fake_1", DateTimeOffset.UtcNow);
        payment.Capture(new Money(10m, new Currency("USD")), "cap_1", DateTimeOffset.UtcNow);
        payment.ClearUncommittedEvents();

        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        repository.Setup(x => x.LoadAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Payment>.Success(payment));

        var references = new Mock<IPaymentReferenceStore>();
        references.Setup(x => x.FindAsync(PaymentProvider.Fake, "fake_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment.Id);

        var store = new Mock<IPaymentReadStore>();
        var handler = new SettlePayment.UseCaseHandler(repository.Object, references.Object, store.Object, TimeProvider.System);

        var result = await handler.Handle(
            new SettlePayment.UseCase(
                PaymentProvider.Fake, "fake_1", PaymentProviderEventType.Captured,
                new Money(10m, new Currency("USD")), null, "evt_1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("captured");
        repository.Verify(x => x.SaveAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
