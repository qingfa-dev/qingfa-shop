using FluentValidation;

using Microsoft.Extensions.Options;

namespace Application.BuildingBlocks.UseCases.Validation.FluentValidation;

/// <summary>Options validation helpers backed by FluentValidation validators.</summary>
public static class OptionsBuilderValidationExtensions
{
    /// <summary>
    /// Validates options through the <see cref="IValidator{TOptions}"/> registered in DI.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="builder">The options builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static OptionsBuilder<TOptions> ValidateFluentValidation<TOptions>(
        this OptionsBuilder<TOptions> builder)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Validate<IValidator<TOptions>>(
            (options, validator) => validator.Validate(options).IsValid,
            "Options failed FluentValidation.");
    }
}
