using AwesomeAssertions;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

namespace Core.Unit.Tests.Modules.Payments.Intents;

public sealed class PaymentSpec
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    private static Money Usd(decimal amount) => new(amount, new Currency("USD"));

    private static Payment NewPayment(PaymentCaptureMode mode = PaymentCaptureMode.Automatic)
    {
        var result = PaymentFactory.Initiate(
            id: Guid.NewGuid(),
            provider: PaymentProvider.Fake,
            providerReference: "fake_ref_1",
            amount: Usd(49.90m),
            captureMode: mode,
            externalReference: "cart-1",
            idempotencyKey: "key-1",
            idempotencyFingerprint: "fp-1",
            customerId: Guid.NewGuid(),
            approvalUrl: "https://pay.example/approve",
            expiresAt: Now.AddMinutes(30),
            nowUtc: Now);

        result.IsSuccess.Should().BeTrue(result.ToString());
        return result.Value;
    }

    private static Payment Refunded()
    {
        Payment payment = NewPayment();
        payment.MarkProcessing(null, Now);
        payment.Capture(Usd(49.90m), "cap_1", Now);

        Guid refundId = Guid.NewGuid();
        payment.RequestRefund(refundId, Usd(49.90m), "damaged", "rk-1", Now);
        payment.CompleteRefund(refundId, "re_1", Now);
        return payment;
    }

    [Fact]
    public void Initiate_ShouldCreatePendingPaymentWithOneEvent()
    {
        Payment payment = NewPayment();

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.UncommittedEvents.Should().ContainSingle();
        payment.Version.Should().Be(1);
    }

    [Fact]
    public void Capture_AutomaticFromPending_ShouldSucceed()
    {
        Payment payment = NewPayment();

        payment.Capture(Usd(49.90m), "cap_1", Now).IsSuccess.Should().BeTrue();

        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.CapturedAmount.Amount.Should().Be(49.90m);
    }

    [Fact]
    public void ManualCapture_ShouldAuthorizeThenCapture()
    {
        Payment payment = NewPayment(PaymentCaptureMode.Manual);

        payment.MarkProcessing(null, Now).IsSuccess.Should().BeTrue();
        payment.Authorize("auth_1", Usd(49.90m), Now).IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Authorized);

        payment.Capture(Usd(49.90m), "cap_1", Now).IsSuccess.Should().BeTrue();

        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.CapturedAmount.Amount.Should().Be(49.90m);
    }

    [Fact]
    public void PartialCapture_ShouldStayAuthorized()
    {
        Payment payment = NewPayment(PaymentCaptureMode.Manual);
        payment.MarkProcessing(null, Now);
        payment.Authorize("auth_1", Usd(49.90m), Now);

        payment.Capture(Usd(20m), "cap_1", Now).IsSuccess.Should().BeTrue();

        payment.Status.Should().Be(PaymentStatus.Authorized);
        payment.CapturedAmount.Amount.Should().Be(20m);
    }

    [Fact]
    public void Initiate_ZeroAmount_ShouldFail()
    {
        var result = PaymentFactory.Initiate(
            Guid.NewGuid(), PaymentProvider.Fake, "ref", Usd(0m), PaymentCaptureMode.Automatic,
            "cart-1", "key-1", "fp-1", Guid.NewGuid(), null, null, Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.AmountInvalid);
    }

    [Fact]
    public void Capture_AboveRemaining_ShouldFail()
    {
        Payment payment = NewPayment(PaymentCaptureMode.Manual);
        payment.MarkProcessing(null, Now);
        payment.Authorize("auth_1", Usd(49.90m), Now);
        payment.Capture(Usd(20m), "cap_1", Now);

        var result = payment.Capture(Usd(30m), "cap_2", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.AmountInvalid);
        payment.CapturedAmount.Amount.Should().Be(20m);
    }

    [Fact]
    public void Authorize_WithDifferentCurrency_ShouldFail()
    {
        Payment payment = NewPayment(PaymentCaptureMode.Manual);
        payment.MarkProcessing(null, Now);

        var result = payment.Authorize("auth_1", new Money(49.90m, new Currency("EUR")), Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.CurrencyMismatch);
    }

    [Fact]
    public void Failed_ThenLateCapture_ShouldSettleWithReason()
    {
        Payment payment = NewPayment();
        payment.Fail("provider_declined", "declined", Now);
        payment.Capture(Usd(49.90m), "cap_late", Now).IsSuccess.Should().BeTrue();

        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.UncommittedEvents.OfType<PaymentCaptured>()
            .Single().Reason.Should().Be(PaymentConstant.Defaults.SettledAfterFailure);
    }

    [Fact]
    public void Cancel_AfterCapture_ShouldFail()
    {
        Payment payment = NewPayment();
        payment.MarkProcessing(null, Now);
        payment.Capture(Usd(49.90m), "cap_1", Now);

        var result = payment.Cancel("changed mind", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.InvalidState);
    }

    [Fact]
    public void Expire_WhenNotPending_ShouldFail()
    {
        Payment payment = NewPayment();
        payment.MarkProcessing(null, Now);

        var result = payment.Expire(Now.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.InvalidState);
    }

    [Fact]
    public void Initiate_MissingReference_ShouldFail()
    {
        var result = PaymentFactory.Initiate(
            Guid.NewGuid(), PaymentProvider.Fake, "ref", Usd(10m), PaymentCaptureMode.Automatic,
            "   ", "key-1", "fp-1", Guid.NewGuid(), null, null, Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.ReferenceRequired);
    }

    [Fact]
    public void Capture_WithoutProviderReference_ShouldFail()
    {
        Payment payment = NewPayment();

        var result = payment.Capture(Usd(10m), "  ", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.ProviderReferenceMissing);
    }

    [Fact]
    public void Capture_ManualBeforeAuthorization_ShouldFail()
    {
        Payment payment = NewPayment(PaymentCaptureMode.Manual);

        var result = payment.Capture(Usd(10m), "cap_1", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.InvalidState);
    }

    [Fact]
    public void ApplyObservation_Authorized_ShouldRequireManualMode()
    {
        Payment manual = NewPayment(PaymentCaptureMode.Manual);
        manual.MarkProcessing(null, Now);

        manual.ApplyObservation(PaymentObservation.Authorized, "auth_1", Usd(49.90m), null, Now)
            .IsSuccess.Should().BeTrue();
        manual.Status.Should().Be(PaymentStatus.Authorized);

        Payment automatic = NewPayment();
        automatic.ApplyObservation(PaymentObservation.Authorized, "auth_2", Usd(49.90m), null, Now)
            .IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(PaymentObservation.Captured, PaymentStatus.Captured)]
    [InlineData(PaymentObservation.Failed, PaymentStatus.Failed)]
    [InlineData(PaymentObservation.Cancelled, PaymentStatus.Cancelled)]
    [InlineData(PaymentObservation.Expired, PaymentStatus.Expired)]
    public void ApplyObservation_ShouldMapToTheRightStatus(
        PaymentObservation observation,
        PaymentStatus expected)
    {
        Payment payment = NewPayment();
        payment.MarkProcessing(null, Now);

        payment.ApplyObservation(observation, "ref_1", Usd(49.90m), "declined", Now)
            .IsSuccess.Should().BeTrue();

        payment.Status.Should().Be(expected);
    }

    [Fact]
    public void RemainingCapturable_ShouldClampAtZero()
    {
        Payment payment = NewPayment();
        payment.MarkProcessing(null, Now);
        payment.Capture(Usd(49.90m), "cap_1", Now);

        payment.RemainingCapturable.Should().Be(0m);
    }

    [Fact]
    public void Expire_ThenLateCapture_ShouldSettle()
    {
        Payment payment = NewPayment();
        payment.Expire(Now.AddHours(1)).IsSuccess.Should().BeTrue();

        payment.Capture(Usd(49.90m), "cap_late", Now.AddHours(2)).IsSuccess.Should().BeTrue();

        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.UncommittedEvents.OfType<PaymentCaptured>()
            .Single().Reason.Should().Be(PaymentConstant.Defaults.LateSettlement);
    }

    [Fact]
    public void Capture_OnCancelled_ShouldFail()
    {
        Payment payment = NewPayment();
        payment.Cancel("changed mind", Now).IsSuccess.Should().BeTrue();

        var result = payment.Capture(Usd(49.90m), "cap_1", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.InvalidState);
        payment.Status.Should().Be(PaymentStatus.Cancelled);
    }

    [Fact]
    public void Capture_OnRefunded_ShouldFail()
    {
        Payment payment = Refunded();
        payment.Status.Should().Be(PaymentStatus.Refunded);

        var result = payment.Capture(Usd(49.90m), "cap_1", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.InvalidState);
    }

    [Fact]
    public void ApplyObservation_Unknown_ShouldFailWithoutAnEvent()
    {
        Payment payment = NewPayment();
        payment.MarkProcessing(null, Now);
        int eventsBefore = payment.UncommittedEvents.Count;

        var result = payment.ApplyObservation((PaymentObservation)99, "ref_1", Usd(1m), null, Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.InvalidState);
        payment.UncommittedEvents.Should().HaveCount(eventsBefore);
    }

    [Fact]
    public void Initiate_NegativeAmount_ShouldFail()
    {
        var result = PaymentFactory.Initiate(
            Guid.NewGuid(), PaymentProvider.Fake, "ref", Usd(-1m), PaymentCaptureMode.Automatic,
            "cart-1", "key-1", "fp-1", Guid.NewGuid(), null, null, Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.AmountInvalid);
    }

    [Fact]
    public void Capture_WithDifferentCurrency_ShouldFail()
    {
        Payment payment = NewPayment();

        var result = payment.Capture(new Money(49.90m, new Currency("EUR")), "cap_1", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.CurrencyMismatch);
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Capture_AutomaticPartial_ShouldFail()
    {
        Payment payment = NewPayment();

        var result = payment.Capture(Usd(20m), "cap_1", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.AmountInvalid);
        payment.CapturedAmount.Amount.Should().Be(0m);
    }

    [Fact]
    public void Capture_LatePartial_ShouldFail()
    {
        Payment payment = NewPayment();
        payment.Expire(Now.AddHours(1)).IsSuccess.Should().BeTrue();

        var result = payment.Capture(Usd(20m), "cap_late", Now.AddHours(2));

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.AmountInvalid);
        payment.Status.Should().Be(PaymentStatus.Expired);
        payment.CapturedAmount.Amount.Should().Be(0m);
    }

    [Fact]
    public void PartialCapture_ThenRemainder_ShouldReachCaptured()
    {
        Payment payment = NewPayment(PaymentCaptureMode.Manual);
        payment.MarkProcessing(null, Now);
        payment.Authorize("auth_1", Usd(49.90m), Now);

        payment.Capture(Usd(20m), "cap_1", Now).IsSuccess.Should().BeTrue();
        payment.AuthorizedAmount.Amount.Should().Be(49.90m);

        payment.Capture(Usd(29.90m), "cap_2", Now).IsSuccess.Should().BeTrue();

        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.CapturedAmount.Amount.Should().Be(49.90m);
        payment.AuthorizedAmount.Amount.Should().Be(49.90m);
        payment.RemainingCapturable.Should().Be(0m);
    }

    [Fact]
    public void Expire_WithoutExpiryInstant_ShouldFail()
    {
        Payment payment = PaymentFactory.Initiate(
            Guid.NewGuid(), PaymentProvider.Fake, "ref", Usd(10m), PaymentCaptureMode.Automatic,
            "cart-1", "key-1", "fp-1", Guid.NewGuid(), null, null, Now).Value;

        var result = payment.Expire(Now.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.InvalidState);
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Expire_BeforeExpiryInstant_ShouldFail()
    {
        Payment payment = NewPayment();

        var result = payment.Expire(Now.AddMinutes(29));

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.InvalidState);
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Expire_AtExpiryInstant_ShouldSucceed()
    {
        Payment payment = NewPayment();

        var result = payment.Expire(Now.AddMinutes(30));

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Expired);
    }

    [Fact]
    public void Initiate_MissingIdempotencyKey_ShouldFail()
    {
        var result = PaymentFactory.Initiate(
            Guid.NewGuid(), PaymentProvider.Fake, "ref", Usd(10m), PaymentCaptureMode.Automatic,
            "cart-1", "   ", "fp-1", Guid.NewGuid(), null, null, Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.IdempotencyKeyRequired);
    }

    [Fact]
    public void Initiate_ReferenceTooLong_ShouldFail()
    {
        var result = PaymentFactory.Initiate(
            Guid.NewGuid(), PaymentProvider.Fake, "ref", Usd(10m), PaymentCaptureMode.Automatic,
            new string('x', PaymentConstant.Constraints.ReferenceMaxLength + 1),
            "key-1", "fp-1", Guid.NewGuid(), null, null, Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.ReferenceTooLong);
    }

    [Fact]
    public void Initiate_IdempotencyKeyTooLong_ShouldFail()
    {
        var result = PaymentFactory.Initiate(
            Guid.NewGuid(), PaymentProvider.Fake, "ref", Usd(10m), PaymentCaptureMode.Automatic,
            "cart-1", new string('k', PaymentConstant.Constraints.IdempotencyKeyMaxLength + 1),
            "fp-1", Guid.NewGuid(), null, null, Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.IdempotencyKeyTooLong);
    }
}