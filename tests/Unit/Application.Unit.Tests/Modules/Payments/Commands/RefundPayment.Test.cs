using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;
using Application.Modules.Payments.Refunds.Commands;

using AwesomeAssertions;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;
using Core.Modules.Payments.Refunds;

using Moq;

namespace Application.Unit.Tests.Modules.Payments.Commands;

public sealed class RefundPaymentTest
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    private static Money Usd(decimal amount) => new(amount, new Currency("USD"));

    private static Payment Captured()
    {
        Payment payment = PaymentFactory.Initiate(
            Guid.NewGuid(), PaymentProvider.Fake, "fake_1", Usd(50m),
            PaymentCaptureMode.Automatic, "cart-1", "key-1", "fp-1", Guid.NewGuid(),
            null, null, Now).Value;

        payment.Capture(Usd(50m), "fake_1", Now);
        payment.ClearUncommittedEvents();
        return payment;
    }

    private static Mock<IPaymentGateway> Gateway(bool supportsPartialRefund)
    {
        var gateway = new Mock<IPaymentGateway>();

        gateway.SetupGet(x => x.Provider).Returns(PaymentProvider.PayPal);
        gateway.SetupGet(x => x.Capabilities).Returns(new PaymentGatewayCapabilities
        {
            SupportsPartialRefund = supportsPartialRefund,
            SupportsManualCapture = true,
            SupportsWebhooks = true
        });
        gateway.Setup(x => x.RefundAsync(It.IsAny<PaymentRefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentRefundRequest request, CancellationToken _) =>
                Result<PaymentRefundResult>.Success(
                    new PaymentRefundResult($"re_{request.RefundId:N}", request.Amount, true, null)));

        return gateway;
    }

    private static Mock<IPaymentGatewayRegistry> Registry(IPaymentGateway gateway)
    {
        var registry = new Mock<IPaymentGatewayRegistry>();
        registry.Setup(x => x.Resolve(It.IsAny<PaymentProvider>()))
            .Returns(Result<IPaymentGateway>.Success(gateway));
        return registry;
    }

    [Fact]
    public async Task Handle_PartialRefundWithoutCapability_ShouldReturnPartialUnsupported()
    {
        Payment payment = Captured();
        Mock<IPaymentGateway> gateway = Gateway(supportsPartialRefund: false);

        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        repository.Setup(x => x.LoadAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Payment>.Success(payment));

        var handler = new RefundPayment.UseCaseHandler(
            repository.Object, Registry(gateway.Object).Object, TimeProvider.System);

        var result = await handler.Handle(
            new RefundPayment.UseCase(
                payment.Id, new RefundPayment.Request(Usd(20m), "damaged"), "rk-1"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.PartialRefundUnsupported);
        gateway.Verify(
            x => x.RefundAsync(It.IsAny<PaymentRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_PartialRefundWithCapability_ShouldCompleteTheRefund()
    {
        Payment payment = Captured();
        Mock<IPaymentGateway> gateway = Gateway(supportsPartialRefund: true);

        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        repository.Setup(x => x.LoadAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Payment>.Success(payment));
        repository.Setup(x => x.SaveAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var handler = new RefundPayment.UseCaseHandler(
            repository.Object, Registry(gateway.Object).Object, TimeProvider.System);

        var result = await handler.Handle(
            new RefundPayment.UseCase(
                payment.Id, new RefundPayment.Request(Usd(20m), "damaged"), "rk-partial"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(20m);
        result.Value.Status.Should().Be(RefundStatus.Succeeded.ToWire());
    }

    [Fact]
    public async Task Handle_FullRefund_ShouldCompleteTheRefund()
    {
        Payment payment = Captured();
        var gateway = Gateway(supportsPartialRefund: false);

        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        repository.Setup(x => x.LoadAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Payment>.Success(payment));
        repository.Setup(x => x.SaveAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var handler = new RefundPayment.UseCaseHandler(
            repository.Object, Registry(gateway.Object).Object, TimeProvider.System);

        var result = await handler.Handle(
            new RefundPayment.UseCase(
                payment.Id, new RefundPayment.Request(Usd(50m), "returned"), "rk-2"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(RefundStatus.Succeeded.ToWire());
        result.Value.Amount.Should().Be(50m);
        result.StatusCode.Should().Be(ResultConstant.StatusCode.Created);
    }

    [Fact]
    public async Task Handle_ProviderFailure_ShouldRecordFailedRefundAndNotMoveTotals()
    {
        Payment payment = Captured();
        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(x => x.Provider).Returns(PaymentProvider.PayPal);
        gateway.Setup(x => x.RefundAsync(It.IsAny<PaymentRefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentRefundResult>.Success(
                new PaymentRefundResult("re_1", Usd(50m), false, "refund_declined")));

        Payment? saved = null;
        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        repository.Setup(x => x.LoadAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Payment>.Success(payment));
        repository.Setup(x => x.SaveAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((value, _) => saved = value)
            .ReturnsAsync(Result.Ok());

        var handler = new RefundPayment.UseCaseHandler(
            repository.Object, Registry(gateway.Object).Object, TimeProvider.System);

        var result = await handler.Handle(
            new RefundPayment.UseCase(
                payment.Id, new RefundPayment.Request(Usd(50m), "returned"), "rk-3"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(RefundStatus.Failed.ToWire());
        saved.Should().NotBeNull();
        saved!.RefundedAmount.Amount.Should().Be(0m);
        saved.Status.Should().Be(PaymentStatus.Captured);
    }
}
