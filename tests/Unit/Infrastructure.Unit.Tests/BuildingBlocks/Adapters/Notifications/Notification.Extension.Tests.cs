using FluentValidation;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Infrastructure.BuildingBlocks.Adapters.Notifications;

using Application.BuildingBlocks.Ports.Notifications.Options;
using Application.BuildingBlocks.Ports.Notifications.Providers;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Notifications;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Notifications")]
public class NotificationExtensionTests
{
    private static WebApplicationBuilder CreateBaseBuilder()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        builder.Services.AddSingleton(configuration);
        builder.Services.AddLogging();
        return builder;
    }

    #region AddNotificationSetting

    [Fact(DisplayName = "AddNotificationSetting should register IOptions<NotificationSetting>")]
    public void AddNotificationSetting_ShouldRegisterIOptions()
    {
        WebApplicationBuilder builder = CreateBaseBuilder();
        builder.AddNotificationSetting();
        ServiceProvider provider = builder.Services.BuildServiceProvider();

        IOptions<NotificationSetting>? options = provider.GetService<IOptions<NotificationSetting>>();

        options.Should().NotBeNull();
    }

    [Fact(DisplayName = "AddNotificationSetting should register IValidator<NotificationSetting>")]
    public void AddNotificationSetting_ShouldRegisterValidator()
    {
        WebApplicationBuilder builder = CreateBaseBuilder();
        builder.AddNotificationSetting();
        ServiceProvider provider = builder.Services.BuildServiceProvider();

        IValidator<NotificationSetting>? validator = provider.GetService<IValidator<NotificationSetting>>();

        validator.Should().NotBeNull();
        validator.Should().BeOfType<NotificationSettingValidator>();
    }

    [Fact(DisplayName = "AddNotificationSetting should register IValidateOptions<NotificationSetting>")]
    public void AddNotificationSetting_ShouldRegisterValidateOptions()
    {
        WebApplicationBuilder builder = CreateBaseBuilder();
        builder.AddNotificationSetting();
        ServiceProvider provider = builder.Services.BuildServiceProvider();

        IEnumerable<IValidateOptions<NotificationSetting>> validators =
            provider.GetServices<IValidateOptions<NotificationSetting>>();

        validators.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "AddNotificationSetting should resolve with default values")]
    public void AddNotificationSetting_ShouldResolveDefaultValues()
    {
        WebApplicationBuilder builder = CreateBaseBuilder();
        builder.AddNotificationSetting();
        ServiceProvider provider = builder.Services.BuildServiceProvider();

        IOptions<NotificationSetting> options = provider.GetRequiredService<IOptions<NotificationSetting>>();

        options.Value.ApplicationName.Should().Be("ReSys Shop");
    }

    #endregion

    #region AddNotificationSetting

    [Fact(DisplayName = "AddNotificationSetting should register IOptions<EmailChannelSetting>")]
    public void AddNotificationSetting_ShouldRegisterEmailChannelSetting()
    {
        WebApplicationBuilder builder = CreateBaseBuilder();
        builder.AddNotificationSetting();
        ServiceProvider provider = builder.Services.BuildServiceProvider();

        IOptions<EmailChannelSetting>? options = provider.GetService<IOptions<EmailChannelSetting>>();

        options.Should().NotBeNull();
    }

    [Fact(DisplayName = "AddNotificationSetting should register IOptions<SmsChannelSetting>")]
    public void AddNotificationSetting_ShouldRegisterSmsChannelSetting()
    {
        WebApplicationBuilder builder = CreateBaseBuilder();
        builder.AddNotificationSetting();
        ServiceProvider provider = builder.Services.BuildServiceProvider();

        IOptions<SmsChannelSetting>? options = provider.GetService<IOptions<SmsChannelSetting>>();

        options.Should().NotBeNull();
    }

    [Fact(DisplayName = "AddNotificationSetting should register IOptions<SendGridProviderSetting>")]
    public void AddNotificationSetting_ShouldRegisterSendGridProviderSetting()
    {
        WebApplicationBuilder builder = CreateBaseBuilder();
        builder.AddNotificationSetting();
        ServiceProvider provider = builder.Services.BuildServiceProvider();

        IOptions<SendGridProviderSetting>? options = provider.GetService<IOptions<SendGridProviderSetting>>();

        options.Should().NotBeNull();
    }

    [Fact(DisplayName = "AddNotificationSetting should register IOptions<SmtpProviderSetting>")]
    public void AddNotificationSetting_ShouldRegisterSmtpProviderSetting()
    {
        WebApplicationBuilder builder = CreateBaseBuilder();
        builder.AddNotificationSetting();
        ServiceProvider provider = builder.Services.BuildServiceProvider();

        IOptions<SmtpProviderSetting>? options = provider.GetService<IOptions<SmtpProviderSetting>>();

        options.Should().NotBeNull();
    }

    [Fact(DisplayName = "AddNotificationSetting should register IOptions<SinchProviderSetting>")]
    public void AddNotificationSetting_ShouldRegisterSinchProviderSetting()
    {
        WebApplicationBuilder builder = CreateBaseBuilder();
        builder.AddNotificationSetting();
        ServiceProvider provider = builder.Services.BuildServiceProvider();

        IOptions<SinchProviderSetting>? options = provider.GetService<IOptions<SinchProviderSetting>>();

        options.Should().NotBeNull();
    }

    #endregion

    #region AddNotifications

    [Fact(DisplayName = "AddNotifications should register INotificationHub")]
    public void AddNotifications_ShouldRegisterNotificationHub()
    {
        WebApplicationBuilder builder = CreateBaseBuilder();
        builder.AddNotifications();
        ServiceProvider provider = builder.Services.BuildServiceProvider();

        INotificationHub? hub = provider.GetService<INotificationHub>();

        hub.Should().NotBeNull();
        hub.Should().BeOfType<NotificationHub>();
    }

    [Fact(DisplayName = "AddNotifications should register INotificationService")]
    public void AddNotifications_ShouldRegisterNotificationService()
    {
        WebApplicationBuilder builder = CreateBaseBuilder();
        builder.AddNotifications();

        builder.Services.Should().Contain(
            s => s.ServiceType == typeof(INotificationService)
                 && s.ImplementationType == typeof(NotificationService));
    }

    [Fact(DisplayName = "AddNotifications should register LoggingProvider as INotificationProvider")]
    public void AddNotifications_ShouldRegisterLoggingProvider()
    {
        WebApplicationBuilder builder = CreateBaseBuilder();
        builder.AddNotifications();
        ServiceProvider provider = builder.Services.BuildServiceProvider();

        IEnumerable<INotificationProvider> providers = provider.GetServices<INotificationProvider>();
        List<INotificationProvider> loggingProviders = providers
            .Where(p => p.GetType() == typeof(LoggingProvider))
            .ToList();

        loggingProviders.Should().NotBeEmpty();
        loggingProviders.Should().AllBeOfType<LoggingProvider>();
    }

    #endregion
}
