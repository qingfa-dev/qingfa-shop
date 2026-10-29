using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Integrations.Observability.Correlation;

/// <summary>Middleware that ensures every request has a correlation ID for cross-service tracing.</summary>
internal sealed class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ObservabilitySetting _options;
    private readonly ILogger<CorrelationMiddleware> _logger;

    public CorrelationMiddleware(
        RequestDelegate next,
        ObservabilitySetting options,
        ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationContext = context.RequestServices
            .GetRequiredService<ICorrelationContext>();

        var correlationId = context.Request.Headers[_options.CorrelationHeader].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString("N");

        correlationContext.CorrelationId = correlationId;

        context.Response.Headers[_options.CorrelationHeader] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }
}
