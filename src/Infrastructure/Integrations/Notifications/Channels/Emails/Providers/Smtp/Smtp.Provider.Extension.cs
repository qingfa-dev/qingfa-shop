using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Integrations.Notifications.Channels.Emails.Providers.Smtp;

/// <summary>Registers the SMTP notification provider with the DI container.</summary>
public static class SmtpProviderExtensions
{
    /// <summary>Adds the SMTP email provider as a scoped INotificationProvider service.</summary>
    public static WebApplicationBuilder AddEmailSmtpProvider(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddScoped<INotificationProvider, SmtpProvider>();
        return builder;
    }
}
