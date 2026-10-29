using FluentValidation;

namespace Infrastructure.Integrations.Observability;

public sealed class ObservabilitySettingValidator : AbstractValidator<ObservabilitySetting>
{
    public ObservabilitySettingValidator()
    {
        RuleFor(x => x.CorrelationHeader)
            .NotEmpty()
            .WithErrorCode(ObservabilitySettingResult.Failure.CorrelationHeaderEmpty.Code)
            .WithMessage(ObservabilitySettingResult.Failure.CorrelationHeaderEmpty.Message)
            .Matches(ObservabilityConstant.Patterns.CorrelationHeader)
            .WithErrorCode(ObservabilitySettingResult.Failure.CorrelationHeaderInvalid.Code)
            .WithMessage(ObservabilitySettingResult.Failure.CorrelationHeaderInvalid.Message);

        RuleFor(x => x.ServiceName)
            .NotEmpty()
            .WithErrorCode(ObservabilitySettingResult.Failure.ServiceNameEmpty.Code)
            .WithMessage(ObservabilitySettingResult.Failure.ServiceNameEmpty.Message);
    }
}
