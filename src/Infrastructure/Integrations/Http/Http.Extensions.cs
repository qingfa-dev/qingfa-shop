using FluentValidation;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Application.Common.Validation.FluentValidation;
using Infrastructure.Integrations.Http.Options;

namespace Infrastructure.Integrations.Http;

public static class HttpClientExtensions
{
    public static WebApplicationBuilder AddHttpClients(
        this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(HttpOptions.SectionName);
        var options = section.Get<HttpOptions>() ?? new HttpOptions();

        builder.Services.AddSingleton<IValidator<HttpOptions>, HttpOptionsValidator>();
        builder.Services
            .AddOptions<HttpOptions>()
            .BindConfiguration(HttpOptions.SectionName)
            .ValidateFluentValidation()
            .ValidateOnStart();

        builder.Services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<HttpOptions>>().Value);

        builder.Services.AddTransient<CorrelationIdPropagationHandler>();

        foreach (var kvp in options.Clients)
        {
            var cfg = kvp.Value;

            builder.Services.AddHttpClient(kvp.Key, c =>
            {
                ConfigureClient(c, cfg, options.DefaultTimeoutSeconds);
            })
            .ApplyCorrelation(options)
            .ApplyResilience(cfg, options);
        }

        return builder;
    }

    public static IServiceCollection AddTypedHttpClient<TClient>(
        this IServiceCollection services,
        string baseAddress,
        Action<HttpClient>? configure = null,
        bool attachResilience = true)
        where TClient : class
    {
        var builder = services.AddHttpClient<TClient>(c =>
        {
            c.BaseAddress = new Uri(baseAddress);
            configure?.Invoke(c);
        })
        .AddHttpMessageHandler<CorrelationIdPropagationHandler>();

        if (attachResilience)
            builder.AddResilienceHandler(
                ResilienceExtensions.DefaultPipelineName,
                _ => { });

        return services;
    }

    public static IServiceCollection AddTypedHttpClient<TService, TImplementation>(
        this IServiceCollection services,
        string baseAddress,
        Action<HttpClient>? configure = null,
        bool attachResilience = true)
        where TService : class
        where TImplementation : class, TService
    {
        var builder = services.AddHttpClient<TService, TImplementation>(c =>
        {
            c.BaseAddress = new Uri(baseAddress);
            configure?.Invoke(c);
        })
        .AddHttpMessageHandler<CorrelationIdPropagationHandler>();

        if (attachResilience)
            builder.AddResilienceHandler(
                ResilienceExtensions.DefaultPipelineName,
                _ => { });

        return services;
    }

    public static IServiceCollection AddTypedHttpClient<TService, TImplementation, THandler>(
        this IServiceCollection services,
        string baseAddress,
        Action<HttpClient>? configure = null,
        bool attachResilience = true)
        where TService : class
        where TImplementation : class, TService
        where THandler : DelegatingHandler
    {
        var builder = services.AddHttpClient<TService, TImplementation>(c =>
        {
            c.BaseAddress = new Uri(baseAddress);
            configure?.Invoke(c);
        })
        .AddHttpMessageHandler<CorrelationIdPropagationHandler>()
        .AddHttpMessageHandler<THandler>();

        if (attachResilience)
            builder.AddResilienceHandler(
                ResilienceExtensions.DefaultPipelineName,
                _ => { });

        return services;
    }

    private static void ConfigureClient(
        HttpClient client,
        NamedClientOptions cfg,
        int defaultTimeout)
    {
        client.BaseAddress = new Uri(cfg.BaseAddress);

        client.Timeout = TimeSpan.FromSeconds(
            cfg.TimeoutSeconds > 0 ? cfg.TimeoutSeconds : defaultTimeout);

        foreach (var h in cfg.DefaultHeaders)
            client.DefaultRequestHeaders.TryAddWithoutValidation(h.Key, h.Value);
    }

    private static IHttpClientBuilder ApplyCorrelation(
        this IHttpClientBuilder builder,
        HttpOptions options)
    {
        if (options.PropagateCorrelationId)
            builder.AddHttpMessageHandler<CorrelationIdPropagationHandler>();

        return builder;
    }

    private static IHttpClientBuilder ApplyResilience(
        this IHttpClientBuilder builder,
        NamedClientOptions cfg,
        HttpOptions options)
    {
        var enabled = options.AttachResiliencePipelineByDefault && cfg.AttachResiliencePipeline;

        if (enabled)
        {
            builder.AddResilienceHandler(
                ResilienceExtensions.DefaultPipelineName,
                _ => { });
        }

        return builder;
    }
}
