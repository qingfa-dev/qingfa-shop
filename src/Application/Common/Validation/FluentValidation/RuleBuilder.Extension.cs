using FluentValidation;

namespace Application.Common.Validation.FluentValidation;

/// <summary>
/// Extends FluentValidation's rule builder with monadic <see cref="Result"/> rules.
/// </summary>
public static class RuleBuilderExtension
{
    /// <summary>
    /// Validates the property with a domain rule that returns a <see cref="Result"/>.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the property.</param>
    /// <param name="validate">The domain rule to run against the property value.</param>
    /// <returns>The same rule chain, so more rules can be appended.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="ruleBuilder"/> or <paramref name="validate"/> is <c>null</c>.
    /// </exception>
    public static IRuleBuilderOptions<T, TProperty> ValidateWith<T, TProperty>(
        this IRuleBuilder<T, TProperty> ruleBuilder,
        Func<TProperty, Result> validate)
    {
        ArgumentNullException.ThrowIfNull(ruleBuilder);
        ArgumentNullException.ThrowIfNull(validate);

        return ruleBuilder.Must((_, value, context) =>
        {
            Result result = validate(value);

            if (result.IsFailure)
            {
                // Map: Stamp FluentValidation's property path onto the domain errors
                // so the shared mapper restores the property name from metadata.
                context.AddResultErrors(
                    result.MapError(error => error.WithField(context.PropertyPath)));
            }

            // Handle: Failures are added above; the predicate itself always passes.
            return true;
        });
    }
}