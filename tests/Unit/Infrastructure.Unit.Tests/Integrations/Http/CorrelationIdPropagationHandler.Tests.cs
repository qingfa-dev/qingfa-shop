using Microsoft.AspNetCore.Http;

using Infrastructure.Integrations.Http;

namespace Infrastructure.Unit.Tests.Integrations.Http;

[Trait("Category", "Unit")]
[Trait("Feature", "Http")]
public class CorrelationIdPropagationHandlerTests
{
    private readonly Mock<HttpContext> _httpContextMock = new();
    private readonly Mock<IHttpContextAccessor> _accessorMock = new();

    public CorrelationIdPropagationHandlerTests()
    {
        _accessorMock.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
    }

    [Fact(DisplayName = "When correlation header present should propagate to outgoing request")]
    public async Task WhenCorrelationHeaderPresent_ShouldPropagate()
    {
        _httpContextMock.Setup(x => x.Request.Headers["X-Correlation-Id"])
            .Returns("correlation-id-123");

        var handler = new CorrelationIdPropagationHandler(_accessorMock.Object)
        {
            InnerHandler = new SucceedingHandler()
        };

        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        request.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        request.Headers.GetValues("X-Correlation-Id")
            .Should().Contain("correlation-id-123");
    }

    [Fact(DisplayName = "When correlation header absent should not add to outgoing request")]
    public async Task WhenCorrelationHeaderAbsent_ShouldNotAdd()
    {
        _httpContextMock.Setup(x => x.Request.Headers["X-Correlation-Id"])
            .Returns(string.Empty);

        var handler = new CorrelationIdPropagationHandler(_accessorMock.Object)
        {
            InnerHandler = new SucceedingHandler()
        };

        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        request.Headers.Contains("X-Correlation-Id").Should().BeFalse();
    }

    [Fact(DisplayName = "When HttpContext null should complete gracefully")]
    public async Task WhenHttpContextNull_ShouldCompleteGracefully()
    {
        _accessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var handler = new CorrelationIdPropagationHandler(_accessorMock.Object)
        {
            InnerHandler = new SucceedingHandler()
        };

        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        request.Headers.Contains("X-Correlation-Id").Should().BeFalse();
    }

    private sealed class SucceedingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken ct)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
