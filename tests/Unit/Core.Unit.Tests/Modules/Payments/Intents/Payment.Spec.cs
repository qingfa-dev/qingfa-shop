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

    [Fact]
    public void Initiate_ShouldCreatePendingPaymentWithOneEvent()
    {
        Payment payment = NewPayment();

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.UncommittedEvents.Should().ContainSingle();
        payment.Version.Should().Be(1);
    }

    [Fact]
    public void Capture_FromPending_ShouldBeRejected()
    {
        Payment payment = NewPayment();

        var result = payment.Capture(Usd(49.90m), "cap_1", Now);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.InvalidState);
        payment.UncommittedEvents.Should().ContainSingle();
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
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.AmountInvalid);
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
    }
}