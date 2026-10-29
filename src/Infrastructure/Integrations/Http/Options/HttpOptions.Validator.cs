using FluentValidation;

namespace Infrastructure.Integrations.Http.Options;

public sealed class HttpOptionsValidator : AbstractValidator<HttpOptions>
{
    public HttpOptionsValidator()
    {
        RuleFor(x => x.DefaultTimeoutSeconds)
            .InclusiveBetween(
                HttpConstant.Constraints.DefaultTimeoutSecondsMin,
                HttpConstant.Constraints.DefaultTimeoutSecondsMax)
            .WithErrorCode(HttpOptionsResult.Failure.DefaultTimeoutSecondsOutOfRange.Code)
            .WithMessage(HttpOptionsResult.Failure.DefaultTimeoutSecondsOutOfRange.Message);

        RuleForEach(x => x.Clients).ChildRules((Action<InlineValidator<KeyValuePair<string, NamedClientOptions>>>)(client =>
        {
            client.RuleFor(x => x.Value.BaseAddress)
                .NotEmpty()
                .WithErrorCode(HttpOptionsResult.Failure.ClientBaseAddressEmpty.Code)
                .WithMessage((string)HttpOptionsResult.Failure.ClientBaseAddressEmpty.Message)
                .Must(u => Uri.TryCreate(u, UriKind.Absolute, out _))
                .WithErrorCode(HttpOptionsResult.Failure.ClientBaseAddressInvalid.Code)
                .WithMessage((string)HttpOptionsResult.Failure.ClientBaseAddressInvalid.Message);

            client.RuleFor(x => x.Value.TimeoutSeconds)
                .GreaterThanOrEqualTo(HttpConstant.Constraints.TimeoutSecondsMin)
                .WithErrorCode(HttpOptionsResult.Failure.ClientTimeoutSecondsNegative.Code)
                .WithMessage((string)HttpOptionsResult.Failure.ClientTimeoutSecondsNegative.Message);
        }));
    }
}
