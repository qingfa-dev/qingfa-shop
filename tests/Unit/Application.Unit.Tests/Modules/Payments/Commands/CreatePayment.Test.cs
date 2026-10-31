using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.Security.Authentication.Contexts;
using Application.Modules.Payments.Intents.Commands;
using Application.Modules.Payments.Persistence;
using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;

using AwesomeAssertions;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Moq;

namespace Application.Unit.Tests.Modules.Payments.Commands;

public sealed class CreatePaymentTest
{
    [Fact]
    public async Task Handle_ReplayWithSameFingerprint_ShouldReturnStoredPayment()
    {
        Guid customerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        Payment existing = PaymentFactory.Initiate(
            Guid.NewGuid(), PaymentProvider.Fake, "fake_1", new Money(10m, new Currency("USD")),
            PaymentCaptureMode.Automatic, "cart-1", "key-1", "fp-1", customerId,
            null, null, DateTimeOffset.UtcNow).Value;

        var idempotency = new Mock<IPaymentIdempotencyStore>();
        idempotency.Setup(x => x.ReserveAsync(customerId, "key-1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IdempotencyReservation>.Success(
                new IdempotencyReservation(existing.Id, IsNew: false, FingerprintMatches: true)));

        var references = new Mock<IPaymentReferenceStore>();
        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        repository.Setup(x => x.LoadAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Payment>.Success(existing));

        var registry = new Mock<IPaymentGatewayRegistry>();
        var user = new Mock<IUserContext>();
        user.SetupGet(x => x.UserId).Returns(customerId.ToString());

        var handler = new CreatePayment.UseCaseHandler(
            repository.Object, idempotency.Object, references.Object, registry.Object, user.Object, TimeProvider.System);

        var request = new CreatePayment.Request(
            new Money(10m, new Currency("USD")), PaymentProvider.Fake, "cart-1",
            PaymentCaptureMode.Automatic, null, null);

        var result = await handler.Handle(new CreatePayment.UseCase(request, "key-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(existing.Id);
        result.Metadata["IdempotentReplay"].Should().Be(true);
        registry.Verify(x => x.Resolve(It.IsAny<PaymentProvider>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProviderDecline_ShouldReturnFailedResource()
    {
        Guid customerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var idempotency = new Mock<IPaymentIdempotencyStore>();
        idempotency.Setup(x => x.ReserveAsync(customerId, "key-2", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IdempotencyReservation>.Success(
                new IdempotencyReservation(Guid.NewGuid(), IsNew: true, FingerprintMatches: true)));

        var references = new Mock<IPaymentReferenceStore>();
        references.Setup(x => x.RecordAsync(
                It.IsAny<PaymentProvider>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(x => x.Provider).Returns(PaymentProvider.Fake);
        gateway.Setup(x => x.CreateIntentAsync(It.IsAny<PaymentIntentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentIntentResult>.Success(new PaymentIntentResult(
                "fake_1", PaymentProviderIntentStatus.Failed, null, null, null, "declined")));

        var registry = new Mock<IPaymentGatewayRegistry>();
        registry.Setup(x => x.Resolve(PaymentProvider.Fake))
            .Returns(Result<IPaymentGateway>.Success(gateway.Object));

        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        repository.Setup(x => x.SaveAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var user = new Mock<IUserContext>();
        user.SetupGet(x => x.UserId).Returns(customerId.ToString());

        var handler = new CreatePayment.UseCaseHandler(
            repository.Object, idempotency.Object, references.Object, registry.Object, user.Object, TimeProvider.System);

        var request = new CreatePayment.Request(
            new Money(10m, new Currency("USD")), PaymentProvider.Fake, "cart-2",
            PaymentCaptureMode.Automatic, null, null);

        var result = await handler.Handle(new CreatePayment.UseCase(request, "key-2"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("failed");
    }
}
