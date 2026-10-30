using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Api.Integration.Tests;

/// <summary>Proves creates and deletes return the status the command and writer chose.</summary>
[Collection(ResultContractStackCollection.Name)]
public sealed class ResultContractFlowApiTest
{
    private readonly ResultContractStackFixture _fixture;

    public ResultContractFlowApiTest(ResultContractStackFixture fixture) => _fixture = fixture;

    [Fact(DisplayName = "An authenticated caller without the permission should return a Result 403")]
    public async Task AuthenticatedWithoutPermission_ShouldReturn403WithResultBody()
    {
        SkipWithoutContainer();

        using HttpClient client = _fixture.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/todos");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using JsonDocument document = await ReadJsonAsync(response);

        document.RootElement.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        document.RootElement.GetProperty("errors").GetArrayLength().Should().BePositive();
        document.RootElement.TryGetProperty("value", out _).Should().BeFalse();
    }

    [Fact(DisplayName = "POST /api/todos should return 201, Location, and the Result value")]
    public async Task CreateTodo_ShouldReturn201WithResultValueAndLocation()
    {
        SkipWithoutContainer();

        using HttpClient client = _fixture.CreateClient("Admin");

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/todos",
            new { title = "Contract create" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using JsonDocument document = await ReadJsonAsync(response);
        JsonElement value = document.RootElement.GetProperty("value");
        Guid id = value.GetProperty("id").GetGuid();

        value.GetProperty("title").GetString().Should().Be("Contract create");
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Be($"/api/todos/{id}");
    }

    [Fact(DisplayName = "DELETE /api/todos/{id} should return 204 with an empty body")]
    public async Task DeleteTodo_ShouldReturn204WithEmptyBody()
    {
        SkipWithoutContainer();

        using HttpClient client = _fixture.CreateClient("Admin");
        Guid id = await CreateTodoAsync(client);

        using HttpResponseMessage response = await client.DeleteAsync(
            $"/api/todos/{id}?mode=Soft");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        string body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        body.Should().BeEmpty();
    }

    [Fact(DisplayName = "A malformed create body should stay outside the Result contract")]
    public async Task CreateTodo_WithMalformedBody_ShouldReturnProblemDetails()
    {
        SkipWithoutContainer();

        using HttpClient client = _fixture.CreateClient("Admin");
        using var content = new StringContent("{", Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync("/api/todos", content);

        // Boundary: minimal-API binding throws BadHttpRequestException before the handler,
        // so the exception handler writes ProblemDetails rather than a Result.
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using JsonDocument document = await ReadJsonAsync(response);

        document.RootElement.TryGetProperty("title", out _).Should().BeTrue();
        document.RootElement.TryGetProperty("isSuccess", out _).Should().BeFalse();
    }

    private static async Task<Guid> CreateTodoAsync(HttpClient client)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/todos",
            new { title = "Contract delete" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using JsonDocument document = await ReadJsonAsync(response);
        return document.RootElement
            .GetProperty("value")
            .GetProperty("id")
            .GetGuid();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        return JsonDocument.Parse(json);
    }

    private void SkipWithoutContainer()
    {
        if (_fixture.SkipReason is { } reason)
            Assert.Skip(reason);
    }
}
