using FluentValidation;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Application.Common.Validation.FluentValidation;

using Infrastructure.Integrations.Notifications.Channels.Logging.Providers;
using Infrastructure.Integrations.Notifications.Services;
using Infrastructure.Integrations.Notifications.Hubs;

namespace Infrastructure.Integrations.Notifications;

/// <summary>
/// Provides extension methods for registering notification system services with the dependency injection container.
/// </summary>
public static class NotificationExtensions
{
    #region Public Registration Methods

    /// <summary>
    /// Registers the complete notification subsystem.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for chaining.</returns>
    public static WebApplicationBuilder AddNotifications(this WebApplicationBuilder builder)
    {
        builder
            .AddNotificationSetting()
            .AddNotificationServices();

        return builder;
    }

    /// <summary>
    /// Registers notification application services, hubs, and providers.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for chaining.</returns>
    public static WebApplicationBuilder AddNotificationServices(this WebApplicationBuilder builder)
    {
        // Register: Unified notification hub with multi-provider fallback delivery
        builder.Services.TryAddScoped<INotificationHub, NotificationHub>();

        // Register: Orchestration service for validation, template resolution, and dispatch
        builder.Services.TryAddScoped<INotificationService, NotificationService>();

        // Register: Development fallback provider for all notification channels
        builder.Services.TryAddScoped<INotificationProvider>(
            sp => new LoggingProvider(
                sp.GetRequiredService<ILogger<LoggingProvider>>(),
                NotificationChannel.SMS));

        return builder;
    }

    /// <summary>
    /// Registers notification options and FluentValidation validators.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for chaining.</returns>
    public static WebApplicationBuilder AddNotificationSetting(this WebApplicationBuilder builder)
    {
        // Register: Root notification settings with FluentValidation (access-time validation)
        builder.Services.AddSingleton<IValidator<NotificationSetting>, NotificationSettingValidator>();
        builder.Services.AddOptions<NotificationSetting>()
            .BindConfiguration(NotificationSetting.Section)
            .ValidateFluentValidation()
            .ValidateOnStart();

        // Register: Email channel settings with FluentValidation (access-time validation)
        builder.Services.AddSingleton<IValidator<EmailChannelSetting>, EmailChannelSettingValidator>();
        builder.Services.AddOptions<EmailChannelSetting>()
            .BindConfiguration(EmailChannelSetting.Section)
            .ValidateFluentValidation()
            .ValidateOnStart();

        // Register: SMS channel settings with FluentValidation (access-time validation)
        builder.Services.AddSingleton<IValidator<SmsChannelSetting>, SmsChannelSettingValidator>();
        builder.Services.AddOptions<SmsChannelSetting>()
            .BindConfiguration(SmsChannelSetting.Section)
            .ValidateFluentValidation()
            .ValidateOnStart();

        // Register: SendGrid provider settings with FluentValidation (access-time validation)
        builder.Services.AddSingleton<IValidator<SendGridProviderSetting>, SendGridProviderSettingValidator>();
        builder.Services.AddOptions<SendGridProviderSetting>()
            .BindConfiguration(SendGridProviderSetting.Section)
            .ValidateFluentValidation()
            .ValidateOnStart();

        // Register: SMTP provider settings with FluentValidation (access-time validation)
        builder.Services.AddSingleton<IValidator<SmtpProviderSetting>, SmtpProviderSettingValidator>();
        builder.Services.AddOptions<SmtpProviderSetting>()
            .BindConfiguration(SmtpProviderSetting.Section)
            .ValidateFluentValidation()
            .ValidateOnStart();

        // Register: Sinch provider settings with FluentValidation (access-time validation)
        builder.Services.AddSingleton<IValidator<SinchProviderSetting>, SinchProviderSettingValidator>();
        builder.Services.AddOptions<SinchProviderSetting>()
            .BindConfiguration(SinchProviderSetting.Section)
            .ValidateFluentValidation()
            .ValidateOnStart();

        return builder;
    }

    #endregion
}