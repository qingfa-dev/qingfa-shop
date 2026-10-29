using FluentValidation;

namespace Application.Common.Integrations.Storages.Security.Guard.Options;

public sealed class AntiForgeryOptionsValidator : AbstractValidator<AntiForgeryOptions>
{
    public AntiForgeryOptionsValidator()
    {
        RuleFor(x => x.MaxConsecutiveFailures)
            .InclusiveBetween(
                AntiForgeryOptionsConstant.Constraints.MaxConsecutiveFailuresMin,
                AntiForgeryOptionsConstant.Constraints.MaxConsecutiveFailuresMax)
            .WithErrorCode(AntiForgeryOptionsResult.Failure.MaxConsecutiveFailuresInvalid.Code)
            .WithMessage(AntiForgeryOptionsResult.Failure.MaxConsecutiveFailuresInvalid.Message);

        RuleFor(x => x.BlockDuration)
            .GreaterThanOrEqualTo(AntiForgeryOptionsConstant.Constraints.BlockDurationMin)
            .WithErrorCode(AntiForgeryOptionsResult.Failure.BlockDurationInvalid.Code)
            .WithMessage(AntiForgeryOptionsResult.Failure.BlockDurationInvalid.Message)
            .LessThanOrEqualTo(AntiForgeryOptionsConstant.Constraints.BlockDurationMax)
            .WithErrorCode(AntiForgeryOptionsResult.Failure.BlockDurationInvalid.Code)
            .WithMessage(AntiForgeryOptionsResult.Failure.BlockDurationInvalid.Message);
    }
}
