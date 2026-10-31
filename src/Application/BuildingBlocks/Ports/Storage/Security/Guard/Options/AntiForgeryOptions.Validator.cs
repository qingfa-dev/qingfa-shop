using FluentValidation;

namespace Application.BuildingBlocks.Ports.Storage.Security.Guard.Options;

public sealed class AntiForgeryOptionsValidator : AbstractValidator<AntiForgeryOptions>
{
    public AntiForgeryOptionsValidator()
    {
        RuleFor(x => x.MaxConsecutiveFailures)
            .InclusiveBetween(
                AntiForgeryOptionsConstant.Constraints.MaxConsecutiveFailuresMin,
                AntiForgeryOptionsConstant.Constraints.MaxConsecutiveFailuresMax)
            .WithErrorCode(AntiForgeryOptionsOutcome.Failure.MaxConsecutiveFailuresInvalid.Code)
            .WithMessage(AntiForgeryOptionsOutcome.Failure.MaxConsecutiveFailuresInvalid.Message);

        RuleFor(x => x.BlockDuration)
            .GreaterThanOrEqualTo(AntiForgeryOptionsConstant.Constraints.BlockDurationMin)
            .WithErrorCode(AntiForgeryOptionsOutcome.Failure.BlockDurationInvalid.Code)
            .WithMessage(AntiForgeryOptionsOutcome.Failure.BlockDurationInvalid.Message)
            .LessThanOrEqualTo(AntiForgeryOptionsConstant.Constraints.BlockDurationMax)
            .WithErrorCode(AntiForgeryOptionsOutcome.Failure.BlockDurationInvalid.Code)
            .WithMessage(AntiForgeryOptionsOutcome.Failure.BlockDurationInvalid.Message);
    }
}
