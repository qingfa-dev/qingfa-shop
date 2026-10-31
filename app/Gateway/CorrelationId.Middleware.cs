namespace Gateway;

/// <summary>
/// Propagates a correlation id across the edge so one caller can be followed
/// through every downstream service's logs.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>The header carrying the correlation id.</summary>
    public const string HeaderName = GatewayConstant.Headers.CorrelationId;

    /// <summary>Reads or mints a correlation id, then echoes it on the response.</summary>
    /// <param name="context">The current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Handle: honour an inbound id so a caller's trace survives the hop; mint one
        // otherwise so no request is unattributable.
        string correlationId = context.Request.Headers.TryGetValue(HeaderName, out var inbound)
            && !string.IsNullOrWhiteSpace(inbound)
                ? inbound.ToString()
                : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        await next(context);
    }
}