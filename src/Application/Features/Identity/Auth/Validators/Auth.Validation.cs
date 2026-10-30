using FluentValidation;

using IdentityConstant = Application.Common.Security.Authentication.Identity.IdentityConstant;

namespace Application.Features.Identity.Auth.Validators;

/// <summary>Reusable FluentValidation rules for Auth requests.</summary>
public static class AuthValidation
{
    /// <summary>Applies the username field rules.</summary>
    /// <typeparam name="T">The use case being validated.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the username.</param>
    /// <returns>The same rule chain, so more rules can be appended.</returns>
    public static IRuleBuilderOptions<T, string> ApplyUserNameRules<T>(
        this IRuleBuilder<T, string> ruleBuilder)
        => ruleBuilder
            .NotEmpty()
            .MinimumLength(IdentityConstant.Constraints.MinUserNameLength)
            .MaximumLength(IdentityConstant.Constraints.MaxUserNameLength);

    /// <summary>Applies the email field rules.</summary>
    /// <typeparam name="T">The use case being validated.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the email address.</param>
    /// <returns>The same rule chain, so more rules can be appended.</returns>
    public static IRuleBuilderOptions<T, string> ApplyEmailRules<T>(
        this IRuleBuilder<T, string> ruleBuilder)
        => ruleBuilder
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(IdentityConstant.Constraints.MaxEmailLength);

    /// <summary>Applies the email field rules to an optional email address.</summary>
    /// <typeparam name="T">The use case being validated.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the optional email address.</param>
    /// <returns>The same rule chain, so more rules can be appended.</returns>
    public static IRuleBuilderOptions<T, string?> ApplyOptionalEmailRules<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
        => ruleBuilder
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(IdentityConstant.Constraints.MaxEmailLength);

    /// <summary>Applies the password field rules.</summary>
    /// <typeparam name="T">The use case being validated.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the password.</param>
    /// <returns>The same rule chain, so more rules can be appended.</returns>
    public static IRuleBuilderOptions<T, string> ApplyPasswordRules<T>(
        this IRuleBuilder<T, string> ruleBuilder)
        => ruleBuilder
            .NotEmpty()
            .MinimumLength(IdentityConstant.Constraints.MinPasswordLength)
            .MaximumLength(IdentityConstant.Constraints.MaxPasswordLength);

    /// <summary>Applies the password field rules to an optional password.</summary>
    /// <typeparam name="T">The use case being validated.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the optional password.</param>
    /// <returns>The same rule chain, so more rules can be appended.</returns>
    public static IRuleBuilderOptions<T, string?> ApplyOptionalPasswordRules<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
        => ruleBuilder
            .NotEmpty()
            .MinimumLength(IdentityConstant.Constraints.MinPasswordLength)
            .MaximumLength(IdentityConstant.Constraints.MaxPasswordLength);

    /// <summary>Applies the token field rules.</summary>
    /// <typeparam name="T">The use case being validated.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the opaque token.</param>
    /// <returns>The same rule chain, so more rules can be appended.</returns>
    public static IRuleBuilderOptions<T, string> ApplyTokenRules<T>(
        this IRuleBuilder<T, string> ruleBuilder)
        => ruleBuilder.NotEmpty();
}
