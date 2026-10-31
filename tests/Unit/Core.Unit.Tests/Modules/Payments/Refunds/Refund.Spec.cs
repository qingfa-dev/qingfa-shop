using AwesomeAssertions;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;
using Core.Modules.Payments.Refunds;

namespace Core.Unit.Tests.Modules.Payments.Refunds;

public sealed class RefundSpec
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);
    private static Money Usd(decimal amount) => new(amount, new Currency("USD"));

    private static Payment Captured()
    {
        Payment payment = PaymentFactory.Initiate(
            Guid.NewGuid(), PaymentProvider.Fake, "ref_1", Usd(50m),
            PaymentCaptureMode.Automatic, "cart-1", "key-1", "fp-1", Guid.NewGuid(),
            null, null, Now).Value;

        payment.MarkProcessing("ref_1", Now);
        payment.Capture(Usd(50m), "cap_1", Now);
        return payment;
    }

    [Fact]
    public void RequestRefund_OverCredit_ShouldFail()
    {
        Payment payment = Captured();

        var result = payment.RequestRefund(Guid.NewGuid(), Usd(51m), "damaged", "rk-1", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.RefundExceedsCredit);
    }

    [Fact]
    public void CompleteRefund_FullAmount_ShouldMarkRefunded()
    {
        Payment payment = Captured();
        Guid refundId = Guid.NewGuid();
        payment.RequestRefund(refundId, Usd(50m), "damaged", "rk-1", Now);

        payment.CompleteRefund(refundId, "re_1", Now).IsSuccess.Should().BeTrue();

        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAmount.Amount.Should().Be(50m);
        payment.Refunds.Single().Status.Should().Be(RefundStatus.Succeeded);
    }

    [Fact]
    public void CompleteRefund_Partial_ShouldMarkPartiallyRefunded()
    {
        Payment payment = Captured();
        Guid refundId = Guid.NewGuid();
        payment.RequestRefund(refundId, Usd(20m), "damaged", "rk-2", Now);

        payment.CompleteRefund(refundId, "re_1", Now);

        payment.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        payment.CreditAllowed.Should().Be(30m);
    }

    [Fact]
    public void RequestRefund_SameIdTwice_ShouldBeANoOp()
    {
        Payment payment = Captured();
        Guid refundId = Guid.NewGuid();
        payment.RequestRefund(refundId, Usd(10m), "damaged", "rk-1", Now);

        var second = payment.RequestRefund(refundId, Usd(10m), "damaged", "rk-1", Now);

        second.IsSuccess.Should().BeTrue();
        payment.Refunds.Should().ContainSingle();
    }

    [Fact]
    public void FailRefund_ShouldLeavePaymentTotalsUnchanged()
    {
        Payment payment = Captured();
        Guid refundId = Guid.NewGuid();
        payment.RequestRefund(refundId, Usd(10m), "damaged", "rk-1", Now);

        payment.FailRefund(refundId, "refund_declined", "declined", Now).IsSuccess.Should().BeTrue();

        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.RefundedAmount.Amount.Should().Be(0m);
        payment.Refunds.Single().Status.Should().Be(RefundStatus.Failed);
    }

    [Fact]
    public void RequestRefund_ZeroAmount_ShouldFail()
    {
        Payment payment = Captured();

        var result = payment.RequestRefund(Guid.NewGuid(), Usd(0m), "damaged", "rk-0", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.AmountInvalid);
    }

    [Fact]
    public void CompleteRefund_UnknownId_ShouldFail()
    {
        Payment payment = Captured();

        var result = payment.CompleteRefund(Guid.NewGuid(), "re_1", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.RefundNotFound);
    }

    [Fact]
    public void CanRefund_ShouldBeFalseAfterFullRefund()
    {
        Payment payment = Captured();
        Guid refundId = Guid.NewGuid();
        payment.RequestRefund(refundId, Usd(50m), "damaged", "rk-1", Now);
        payment.CompleteRefund(refundId, "re_1", Now);

        payment.CanRefund.Should().BeFalse();
    }

    [Fact]
    public void RequestRefund_OnPendingPayment_ShouldFail()
    {
        Payment payment = PaymentFactory.Initiate(
            Guid.NewGuid(), PaymentProvider.Fake, "ref_1", Usd(10m),
            PaymentCaptureMode.Automatic, "cart-1", "key-1", "fp-1", Guid.NewGuid(),
            null, null, Now).Value;

        var result = payment.RequestRefund(Guid.NewGuid(), Usd(5m), "changed mind", "rk-3", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.RefundNotCaptured);
    }
}