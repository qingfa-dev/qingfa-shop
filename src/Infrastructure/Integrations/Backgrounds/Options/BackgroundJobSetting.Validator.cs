using FluentValidation;

using Microsoft.Extensions.Configuration;

using Infrastructure.Caching.Options;

namespace Infrastructure.Integrations.Backgrounds.Options;

/// <summary>
/// Validates BackgroundJobSetting configuration using FluentValidation.
/// Ensures all configuration values meet business requirements and constraints.
/// </summary>
/// <remarks>
/// This validator enforces the following rules:
/// 1. DashboardPath must not be empty
/// 2. DashboardPath must not exceed maximum length
/// 3. When CachingEnabled is true, a valid connection string must exist
/// </remarks>
/// <Boundary>Infrastructure - Configuration validation</Boundary>
public sealed class BackgroundJobSettingValidator : AbstractValidator<BackgroundJobSetting>
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of BackgroundJobSettingValidator.
    /// </summary>
    /// <param name="configuration">The configuration to validate against.</param>
    /// <Contract>configuration != null</Contract>
    public BackgroundJobSettingValidator(IConfiguration configuration)
    {
        // Boundary: Infrastructure/DependencyInjection - Service registration
        _configuration = configuration;

        // Validate DashboardPath is not empty
        RuleFor(x => x.DashboardPath)
            .NotEmpty()
            .WithErrorCode(BackgroundJobSettingResult.Failure.DashboardPathRequired.Code)
            .WithMessage(BackgroundJobSettingResult.Failure.DashboardPathRequired.Message);

        // Validate DashboardPath length constraint
        RuleFor(x => x.DashboardPath)
            .MaximumLength(BackgroundJobDefaults.Constraints.DashboardPathMaxLength)
            .WithErrorCode(BackgroundJobSettingResult.Failure.DashboardPathTooLong.Code)
            .WithMessage(BackgroundJobSettingResult.Failure.DashboardPathTooLong.Message);

        // Conditional validation: when CachingEnabled is true, require connection string
        When(x => x.CachingEnabled, (Action)(() =>
        {
            // Validate: Caching requires valid Redis connection string
            RuleFor(x => x)
                .Must(_ => HasValidConnectionString())
                .WithErrorCode(BackgroundJobSettingResult.Failure.CachingConnectionStringMissing.Code)
                .WithMessage((string)BackgroundJobSettingResult.Failure.CachingConnectionStringMissing.Message);
        }));
    }

    /// <summary>
    /// Validates that a valid Redis connection string exists in configuration.
    /// </summary>
    /// <returns>True if a valid connection string is found, false otherwise.</returns>
    /// <remarks>
    /// This method checks for connection strings in the following order:
    /// 1. Aspire-specific connection string (preferred)
    /// 2. Default connection string
    /// If neither is found, validation fails.
    /// </remarks>
    /// <Exception>Throws no exceptions, returns false for invalid configuration</Exception>
    /// <AgentHint>Complex validation logic with fallback strategy</AgentHint>
    private bool HasValidConnectionString()
    {
        // Check both Aspire and Default connection strings
        var aspire = _configuration.GetConnectionString(CachingSettingConstant.Connections.Aspire);
        if (!string.IsNullOrEmpty(aspire))
            return true;

        var @default = _configuration.GetConnectionString(CachingSettingConstant.Connections.Default);
        return !string.IsNullOrEmpty(@default);
    }
}