using AwesomeAssertions;

using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

namespace Core.Unit.Tests.Modules.Payments.Intents;

/// <summary>Covers the module's static constants and paging clamp.</summary>
public sealed class PaymentConstantSpec
{
    [Theory]
    [InlineData(null, PaymentConstant.Defaults.DefaultPageSize)]
    [InlineData(0, PaymentConstant.Defaults.DefaultPageSize)]
    [InlineData(-5, PaymentConstant.Defaults.DefaultPageSize)]
    [InlineData(1, 1)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    [InlineData(101, PaymentConstant.Constraints.MaxPageSize)]
    [InlineData(10_000, PaymentConstant.Constraints.MaxPageSize)]
    public void ClampSize_ShouldBoundTheRequestedSize(int? requested, int expected)
        => PaymentConstant.Paging.ClampSize(requested).Should().Be(expected);

    [Fact]
    public void Constraints_ShouldNotCollide()
        => PaymentConstant.Constraints.MaxPageSize.Should().BeGreaterThan(
            PaymentConstant.Defaults.DefaultPageSize);

    [Fact]
    public void HttpStatus_ShouldCarryTheDocumentedCodes()
    {
        PaymentConstant.HttpStatus.BadRequest.Should().Be(400);
        PaymentConstant.HttpStatus.Unauthorized.Should().Be(401);
        PaymentConstant.HttpStatus.NotFound.Should().Be(404);
        PaymentConstant.HttpStatus.Conflict.Should().Be(409);
        PaymentConstant.HttpStatus.ContentTooLarge.Should().Be(413);
        PaymentConstant.HttpStatus.UnsupportedMediaType.Should().Be(415);
        PaymentConstant.HttpStatus.BadGateway.Should().Be(502);
        PaymentConstant.HttpStatus.ServiceUnavailable.Should().Be(503);
    }

    [Fact]
    public void Reasons_ShouldBeSnakeCased()
    {
        PaymentConstant.Reasons.LateSettlement.Should().Be("late_settlement");
        PaymentConstant.Reasons.SettledAfterFailure.Should().Be("settled_after_failure");
        PaymentConstant.Reasons.InitialCapture.Should().Be("initial_capture");
        PaymentConstant.Reasons.CustomerCancelled.Should().Be("customer_cancelled");
    }

    [Fact]
    public void OutcomeCodes_ShouldBeUnique()
    {
        string[] codes =
        [
            PaymentsOutcome.Codes.NotFound,
            PaymentsOutcome.Codes.ReferenceNotFound,
            PaymentsOutcome.Codes.InvalidState,
            PaymentsOutcome.Codes.AmountInvalid,
            PaymentsOutcome.Codes.AmountExceedsLimit,
            PaymentsOutcome.Codes.CurrencyMismatch,
            PaymentsOutcome.Codes.ReferenceRequired,
            PaymentsOutcome.Codes.ReferenceTooLong,
            PaymentsOutcome.Codes.IdempotencyKeyRequired,
            PaymentsOutcome.Codes.IdempotencyKeyTooLong,
            PaymentsOutcome.Codes.IdempotencyConflict,
            PaymentsOutcome.Codes.CaptureModeInvalid,
            PaymentsOutcome.Codes.CustomerRequired,
            PaymentsOutcome.Codes.RefundExceedsCredit,
            PaymentsOutcome.Codes.RefundNotCaptured,
            PaymentsOutcome.Codes.RefundNotFound,
            PaymentsOutcome.Codes.RefundReasonRequired,
            PaymentsOutcome.Codes.RefundAlreadySettled,
            PaymentsOutcome.Codes.PartialRefundUnsupported,
            PaymentsOutcome.Codes.ProviderUnknown,
            PaymentsOutcome.Codes.ProviderReferenceMissing,
            PaymentsOutcome.Codes.ProviderDisabled,
            PaymentsOutcome.Codes.ProviderRequestRejected,
            PaymentsOutcome.Codes.ProviderUnavailable,
            PaymentsOutcome.Codes.ProviderDeclined,
            PaymentsOutcome.Codes.ProviderConfigurationInvalid,
            PaymentsOutcome.Codes.WebhookSignatureInvalid,
            PaymentsOutcome.Codes.WebhookContentTypeUnsupported,
            PaymentsOutcome.Codes.WebhookPayloadInvalid,
            PaymentsOutcome.Codes.WebhookPayloadTooLarge,
            PaymentsOutcome.Codes.WebhookProviderUnsupported,
            PaymentsOutcome.Codes.WebhookAmountMismatch
        ];

        codes.Should().OnlyHaveUniqueItems();
        codes.Should().OnlyContain(code => code.StartsWith("Payments.", StringComparison.Ordinal));
    }

    [Fact]
    public void FailureCodeNormalization_ShouldClampToTheColumnWidth()
    {
        string longCode = new('x', PaymentConstant.Constraints.FailureCodeMaxLength + 20);

        PaymentValidator.NormalizeFailureCode(longCode).Should().HaveLength(
            PaymentConstant.Constraints.FailureCodeMaxLength);
        PaymentValidator.NormalizeFailureCode(null).Should().BeNull();
        PaymentValidator.NormalizeFailureCode("   ").Should().BeNull();
        PaymentValidator.NormalizeFailureCode("card_declined").Should().Be("card_declined");
    }
}
