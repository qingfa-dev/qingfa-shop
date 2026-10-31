using Application.Modules.Payments.Ports;

using Infrastructure.BuildingBlocks;
using Infrastructure.Modules.Payments;
using Infrastructure.Modules.Payments.Adapters.Fake;
using Infrastructure.Modules.Payments.Adapters.PayPal;
using Infrastructure.Modules.Payments.Adapters.Stripe;
using Infrastructure.Modules.Payments.Persistence;
using Infrastructure.Modules.Todos.Integrations.Messaging;
using Infrastructure.Modules.Todos.Persistence.EventSourcing;
using Infrastructure.Modules.Todos.Services;

using Infrastructure.Modules.Payments.Retention;
using Infrastructure.Payments.Configuration;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Modules;

/// <summary>Composition root for Infrastructure business modules.</summary>
public static class ModularInfraComposition
{
    /// <summary>Creates the module contributions consumed by BuildingBlocks registration.</summary>
    /// <returns>The module registration set.</returns>
    public static InfrastructureModuleRegistrations CreateRegistrations() =>
        new(
            Messaging: [new TodoMessagingModule()],
            EventStores: [new TodoEventStoreModule(), new PaymentEventStoreModule()]);

    /// <summary>Registers module services.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddModularInfrastructure(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPaymentsProviders();
        builder.AddTodoServices();
        return builder;
    }

    /// <summary>Registers the payment providers that configuration enables.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    private static WebApplicationBuilder AddPaymentsProviders(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<PaymentsOptions>()
            .BindConfiguration(PaymentsOptions.SectionName)
            .ValidateOnStart();

        builder.Services.AddSingleton<IValidateOptions<PaymentsOptions>>(
            new PaymentsOptionsValidator(builder.Environment.EnvironmentName));

        builder.Services.AddOptions<PaymentPolicyOptions>()
            .BindConfiguration(PaymentPolicyOptions.SectionName)
            .ValidateOnStart();

        builder.Services.AddSingleton<IValidateOptions<PaymentPolicyOptions>, PaymentPolicyOptionsValidator>();

        builder.Services.AddOptions<PaymentRetentionOptions>()
            .BindConfiguration(PaymentRetentionOptions.SectionName)
            .ValidateOnStart();

        builder.Services.AddSingleton<IValidateOptions<PaymentRetentionOptions>, PaymentRetentionOptionsValidator>();
        builder.Services.AddHostedService<PaymentWebhookRetentionWorker>();

        builder.Services.AddOptions<StripeOptions>().BindConfiguration(StripeOptions.SectionName);
        builder.Services.AddOptions<PayPalOptions>().BindConfiguration(PayPalOptions.SectionName);

        // Initialize: expose the bound values as plain injectables for the adapter constructors.
        builder.Services.AddSingleton(provider => provider.GetRequiredService<IOptions<StripeOptions>>().Value);
        builder.Services.AddSingleton(provider => provider.GetRequiredService<IOptions<PayPalOptions>>().Value);

        PaymentsOptions payments = builder.Configuration
            .GetSection(PaymentsOptions.SectionName)
            .Get<PaymentsOptions>() ?? new PaymentsOptions();

        // Enforce: a disabled provider must be absent from the registry, so a caller cannot
        // select it by name.
        if (payments.Providers.Fake.Enabled)
            builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();

        if (payments.Providers.Stripe.Enabled)
        {
            builder.Services.AddSingleton<StripePaymentGateway>();
            builder.Services.AddSingleton<IPaymentGateway>(
                provider => provider.GetRequiredService<StripePaymentGateway>());
        }

        if (payments.Providers.PayPal.Enabled)
        {
            builder.Services.AddHttpClient<PayPalTokenProvider>((provider, client) =>
            {
                PayPalOptions paypal = provider.GetRequiredService<IOptions<PayPalOptions>>().Value;
                client.BaseAddress = new Uri(paypal.BaseAddress);
            });

            builder.Services.AddHttpClient<PayPalPaymentGateway>((provider, client) =>
            {
                PayPalOptions paypal = provider.GetRequiredService<IOptions<PayPalOptions>>().Value;
                client.BaseAddress = new Uri(paypal.BaseAddress);
            });

            builder.Services.AddSingleton<IPaymentGateway>(
                provider => provider.GetRequiredService<PayPalPaymentGateway>());
        }

        builder.Services.TryAddSingleton<IPaymentGatewayRegistry, PaymentGatewayRegistry>();

        // Register: the slices enforce the configured ceilings through this port.
        builder.Services.TryAddSingleton<IPaymentPolicy, PaymentPolicy>();

        return builder;
    }

    /// <summary>Adds module pipeline hooks.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseModularInfrastructure(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app;
    }
}
