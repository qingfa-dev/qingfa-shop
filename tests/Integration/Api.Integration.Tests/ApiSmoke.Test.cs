using System.Net;
using System.Net.Http.Json;

using Testing;
using Testing.Fixtures;

namespace Api.Integration.Tests;

[Collection(ApiHostCollection.Name)]
public sealed class ApiSmokeTest : IntegrationTestBase
{
    public ApiSmokeTest(ShopApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetTime_ReturnsSuccess()
    {
        // Act
        using var response = await Client.GetAsync("/time");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<TimeResponse>();
        Assert.NotNull(payload);
    }

    [Fact]
    public async Task GetOpenApiDocument_ReturnsSuccess()
    {
        // Act
        using var response = await Client.GetAsync("/openapi/v1.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLiveHealth_ReturnsSuccess()
    {
        // Act
        using var response = await Client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScheduleReminder_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await Client.PostAsJsonAsync(
            $"/api/todos/{Guid.NewGuid()}/reminders",
            new { remindAtUtc = DateTimeOffset.UtcNow.AddHours(1) });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SearchReminders_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await Client.GetAsync(
            $"/api/todos/{Guid.NewGuid()}/reminders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SnoozeReminder_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await Client.PatchAsJsonAsync(
            $"/api/todos/{Guid.NewGuid()}/reminders/{Guid.NewGuid()}/snooze",
            new { remindAtUtc = DateTimeOffset.UtcNow.AddHours(1) });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTodos_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await Client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTodo_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await Client.PostAsJsonAsync(
            "/api/todos",
            new { title = "blocked" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTodoLists_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await Client.GetAsync("/api/todo-lists");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTodoCategory_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await Client.PostAsJsonAsync(
            "/api/todo-categories",
            new { name = "blocked" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPermissionCatalog_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        using var response = await Client.GetAsync("/api/identity/permissions");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RemovedAuthorizationCatalogRoute_ReturnsNotFound()
    {
        // Act
        using var response = await Client.GetAsync("/api/authorization/permissions");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record TimeResponse(DateTimeOffset UtcNow);
}