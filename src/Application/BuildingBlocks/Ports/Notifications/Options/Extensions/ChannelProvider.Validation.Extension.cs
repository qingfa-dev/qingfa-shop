using System.Text.RegularExpressions;

using FluentValidation;

using Application.BuildingBlocks.Ports.Notifications.Options;
using Application.BuildingBlocks.Ports.Notifications.Options.Providers;

namespace Application.BuildingBlocks.Ports.Notifications.Options.Extensions;

/// <summary>Extension methods for FluentValidation with common channel and provider validation rules.</summary>
public static class ChannelProviderValidationExtensions
{
    /// <summary>Validates that a string is a well-formed HTTP or HTTPS URL.</summary>
    public static IRuleBuilderOptions<T, string> MustBeValidUrl<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        // Validate: URL must be non-empty and parseable as an absolute HTTP/HTTPS URI
        return ruleBuilder
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
                         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
    }

    /// <summary>Validates that a string is a well-formed email address.</summary>
    public static IRuleBuilderOptions<T, string> MustBeValidEmail<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        // Validate: Email must be non-empty and conform to email address format
        return ruleBuilder
            .NotEmpty()
            .EmailAddress();
    }

    /// <summary>Validates that a string matches the configured phone number pattern.</summary>
    public static IRuleBuilderOptions<T, string> MustBeValidPhone<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        // Validate: Phone must be non-empty and match the configured phone regex pattern
        return ruleBuilder
            .NotEmpty()
            .Matches(new Regex(NotificationSettingConstant.Patterns.PhoneNumber, RegexOptions.None, TimeSpan.FromMilliseconds(100)));
    }

    /// <summary>Validates that at least one provider is enabled in the dictionary.</summary>
    public static IRuleBuilderOptions<T, Dictionary<string, BaseProviderSetting>> MustHaveEnabledProvider<T>(
        this IRuleBuilder<T, Dictionary<string, BaseProviderSetting>> ruleBuilder)
    {
        // Validate: Dictionary must contain at least one enabled provider value
        return ruleBuilder
            .Must(dict => dict != null && dict.Values.Any(p => p.Enabled));
    }
}
