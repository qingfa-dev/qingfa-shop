using System.Text.Json;

using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Testing.Fixtures;

using Infrastructure.Integrations.Serialization;

namespace Api.Integration.Tests;

/// <summary>Proves the in-process host uses exactly the serializer the result writer uses.</summary>
[Collection(ApiHostCollection.Name)]
[Trait("Category", "Unit")]
public sealed class ApiJsonHostTest
{
    private readonly ShopApiFactory _factory;

    public ApiJsonHostTest(ShopApiFactory factory) => _factory = factory;

    [Fact]
    public void HostOptions_ShouldMatchApiJsonOptionsForNonAsciiPayload()
    {
        JsonSerializerOptions hostOptions = _factory.Services
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value
            .SerializerOptions;

        const string payload = "héllo ✓ 日本語";

        JsonSerializer.Serialize(payload, hostOptions)
            .Should().Be(JsonSerializer.Serialize(payload, ApiJson.Options));
    }
}
