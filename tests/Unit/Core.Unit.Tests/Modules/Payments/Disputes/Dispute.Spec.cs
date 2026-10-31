using AwesomeAssertions;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Disputes;
using Core.Modules.Payments.Intents;

namespace Core.Unit.Tests.Modules.Payments.Disputes;

/// <summary>Covers the dispute transitions on the payment aggregate.</summary>
public sealed class DisputeSpec
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

    [Fact]
    public void OpenDispute_ShouldRecordAnOpenDispute()
    {
        Payment payment = Captured();
        Guid disputeId = Guid.NewGuid();

        Result<Payment> result = payment.OpenDispute(
            disputeId, "dp_1", Usd(50m), "fraudulent", Now);

        result.IsSuccess.Should().BeTrue();
        payment.Disputes.Should().ContainSingle();
        payment.Disputes[0].Status.Should().Be(DisputeStatus.Opened);
        payment.HasOpenDispute.Should().BeTrue();

        // Enforce: a dispute never moves the captured or refunded totals.
        payment.CapturedAmount.Amount.Should().Be(50m);
        payment.RefundedAmount.Amount.Should().Be(0m);
        payment.Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public void OpenDispute_SameIdTwice_ShouldBeANoOp()
    {
        Payment payment = Captured();
        Guid disputeId = Guid.NewGuid();
        payment.OpenDispute(disputeId, "dp_1", Usd(10m), "fraudulent", Now);

        Result<Payment> second = payment.OpenDispute(disputeId, "dp_1", Usd(10m), "fraudulent", Now);

        second.IsSuccess.Should().BeTrue();
        payment.Disputes.Should().ContainSingle();
    }

    [Fact]
    public void OpenDispute_OnPendingPayment_ShouldFail()
    {
        Payment payment = PaymentFactory.Initiate(
            Guid.NewGuid(), PaymentProvider.Fake, "fake_1", Usd(10m),
            PaymentCaptureMode.Automatic, "cart-1", "key-1", "fp-1", Guid.NewGuid(),
            null, null, Now).Value;

        Result<Payment> result = payment.OpenDispute(
            Guid.NewGuid(), "dp_1", Usd(10m), "fraudulent", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.InvalidState);
    }

    [Fact]
    public void OpenDispute_AboveCaptured_ShouldFail()
    {
        Payment payment = Captured();

        Result<Payment> result = payment.OpenDispute(
            Guid.NewGuid(), "dp_1", Usd(51m), "fraudulent", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.DisputeExceedsCaptured);
    }

    [Fact]
    public void ResolveDispute_ShouldCloseIt()
    {
        Payment payment = Captured();
        Guid disputeId = Guid.NewGuid();
        payment.OpenDispute(disputeId, "dp_1", Usd(50m), "fraudulent", Now);

        payment.ResolveDispute(disputeId, DisputeOutcome.Won, Now.AddDays(1)).IsSuccess.Should().BeTrue();

        payment.Disputes[0].Status.Should().Be(DisputeStatus.Won);
        payment.HasOpenDispute.Should().BeFalse();
        payment.Disputes[0].ResolvedAt.Should().Be(Now.AddDays(1));
    }

    [Fact]
    public void ResolveDispute_ByProviderReference_ShouldWork()
    {
        Payment payment = Captured();
        payment.OpenDispute(Guid.NewGuid(), "dp_1", Usd(50m), "fraudulent", Now);

        Result<Payment> result = payment.ResolveDispute("dp_1", DisputeOutcome.Lost, Now.AddDays(2));

        result.IsSuccess.Should().BeTrue();
        payment.Disputes[0].Status.Should().Be(DisputeStatus.Lost);
    }

    [Fact]
    public void ResolveDispute_UnknownReference_ShouldFail()
    {
        Payment payment = Captured();

        Result<Payment> result = payment.ResolveDispute("dp_missing", DisputeOutcome.Lost, Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.DisputeNotFound);
    }

    [Fact]
    public void DeterministicId_ShouldBeStable()
    {
        Guid first = PaymentDisputeExtension.DeterministicId("dp_1");
        Guid second = PaymentDisputeExtension.DeterministicId("dp_1");

        first.Should().Be(second);
        first.Should().NotBe(PaymentDisputeExtension.DeterministicId("dp_2"));
    }
}
