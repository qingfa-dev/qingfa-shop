using FluentValidation;

namespace Infrastructure.BuildingBlocks.Adapters.Http.Options;

public sealed class HttpOptionsValidator : AbstractValidator<HttpOptions>
{
    public HttpOptionsValidator()
    {
        RuleFor(x => x.DefaultTimeoutSeconds)
            .InclusiveBetween(
                HttpConstant.Constraints.DefaultTimeoutSecondsMin,
                HttpConstant.Constraints.DefaultTimeoutSecondsMax)
            .WithErrorCode(HttpOptionsOutcome.Failure.DefaultTimeoutSecondsOutOfRange.Code)
            .WithMessage(HttpOptionsOutcome.Failure.DefaultTimeoutSecondsOutOfRange.Message);

        RuleForEach(x => x.Clients).ChildRules((Action<InlineValidator<KeyValuePair<string, NamedClientOptions>>>)(client =>
        {
            client.RuleFor(x => x.Value.BaseAddress)
                .NotEmpty()
                .WithErrorCode(HttpOptionsOutcome.Failure.ClientBaseAddressEmpty.Code)
                .WithMessage((string)HttpOptionsOutcome.Failure.ClientBaseAddressEmpty.Message)
                .Must(u => Uri.TryCreate(u, UriKind.Absolute, out _))
                .WithErrorCode(HttpOptionsOutcome.Failure.ClientBaseAddressInvalid.Code)
                .WithMessage((string)HttpOptionsOutcome.Failure.ClientBaseAddressInvalid.Message);

            client.RuleFor(x => x.Value.TimeoutSeconds)
                .GreaterThanOrEqualTo(HttpConstant.Constraints.TimeoutSecondsMin)
                .WithErrorCode(HttpOptionsOutcome.Failure.ClientTimeoutSecondsNegative.Code)
                .WithMessage((string)HttpOptionsOutcome.Failure.ClientTimeoutSecondsNegative.Message);
        }));
    }
}
