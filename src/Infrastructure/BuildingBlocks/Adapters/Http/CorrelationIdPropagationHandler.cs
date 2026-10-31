using Microsoft.AspNetCore.Http;

namespace Infrastructure.BuildingBlocks.Adapters.Http;

/// <summary>Delegating handler that propagates the correlation ID from the incoming request to outgoing HTTP calls.</summary>
internal sealed class CorrelationIdPropagationHandler(IHttpContextAccessor accessor)
    : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        var id = accessor.HttpContext?
            .Request.Headers["X-Correlation-Id"]
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(id))
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", id);

        return base.SendAsync(request, ct);
    }
}
