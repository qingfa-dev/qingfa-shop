using FluentValidation;

namespace Infrastructure.BuildingBlocks.Adapters.Observability;

public sealed class ObservabilitySettingValidator : AbstractValidator<ObservabilitySetting>
{
    public ObservabilitySettingValidator()
    {
        RuleFor(x => x.CorrelationHeader)
            .NotEmpty()
            .WithErrorCode(ObservabilitySettingOutcome.Failure.CorrelationHeaderEmpty.Code)
            .WithMessage(ObservabilitySettingOutcome.Failure.CorrelationHeaderEmpty.Message)
            .Matches(ObservabilityConstant.Patterns.CorrelationHeader)
            .WithErrorCode(ObservabilitySettingOutcome.Failure.CorrelationHeaderInvalid.Code)
            .WithMessage(ObservabilitySettingOutcome.Failure.CorrelationHeaderInvalid.Message);

        RuleFor(x => x.ServiceName)
            .NotEmpty()
            .WithErrorCode(ObservabilitySettingOutcome.Failure.ServiceNameEmpty.Code)
            .WithMessage(ObservabilitySettingOutcome.Failure.ServiceNameEmpty.Message);
    }
}
