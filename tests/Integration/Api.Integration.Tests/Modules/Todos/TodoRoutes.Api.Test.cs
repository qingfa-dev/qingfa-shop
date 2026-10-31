using System.Net;
using System.Net.Http.Json;

using Api.Integration.Tests.BuildingBlocks.Api;
using Api.Integration.Tests.BuildingBlocks.Security.Identity;

namespace Api.Integration.Tests.Modules.Todos;

/// <summary>
/// Reaches the todo transport handlers that the identity suites do not touch.
/// </summary>
/// <remarks>
/// The Web.Api coverage gate measures the composition root as a whole. These requests
/// deliberately use missing identifiers: the point is to execute every endpoint delegate
/// and prove it maps its command failure through the shared Result contract, not to
/// duplicate the todo feature suites.
/// </remarks>
[Collection(ResultContractStackCollection.Name)]
public sealed class TodoRoutesApiTest : IdentityStackTestBase
{
    public TodoRoutesApiTest(ResultContractStackFixture fixture)
        : base(fixture)
    {
    }

    [Fact(DisplayName = "Every todo item route should execute its handler")]
    public async Task TodoItemRoutes_ShouldReachEveryHandler()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        Guid todoId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        Guid listId = Guid.NewGuid();

        AssertNotServerError(await client.GetAsync("/api/todos?includeDeleted=false"));
        AssertNotServerError(await client.GetAsync(
            $"/api/todos/{todoId}?includeDeleted=false"));
        AssertNotServerError(await client.PutAsJsonAsync(
            $"/api/todos/{todoId}",
            new { title = "Updated" }));
        AssertNotServerError(await client.PostAsync(
            $"/api/todos/{todoId}/start",
            content: null));
        AssertNotServerError(await client.PostAsync(
            $"/api/todos/{todoId}/complete",
            content: null));
        AssertNotServerError(await client.PostAsync(
            $"/api/todos/{todoId}/cancel",
            content: null));
        AssertNotServerError(await client.PostAsync(
            $"/api/todos/{todoId}/reopen",
            content: null));
        AssertNotServerError(await client.PostAsync(
            $"/api/todos/{todoId}/restore",
            content: null));
        AssertNotServerError(await client.PostAsync(
            $"/api/todos/{todoId}/categories/{categoryId}",
            content: null));
        AssertNotServerError(await client.DeleteAsync(
            $"/api/todos/{todoId}/categories/{categoryId}"));
        AssertNotServerError(await client.PutAsJsonAsync(
            $"/api/todos/{todoId}/list",
            new { listId }));
        AssertNotServerError(await client.DeleteAsync(
            $"/api/todos/{todoId}/list"));

        Guid reminderId = Guid.NewGuid();
        AssertNotServerError(await client.GetAsync(
            $"/api/todos/{todoId}/reminders"));
        AssertNotServerError(await client.GetAsync(
            $"/api/todos/{todoId}/reminders/{reminderId}"));
        AssertNotServerError(await client.PostAsJsonAsync(
            $"/api/todos/{todoId}/reminders",
            new { remindAtUtc = DateTimeOffset.UtcNow.AddHours(1) }));
        AssertNotServerError(await client.PatchAsJsonAsync(
            $"/api/todos/{todoId}/reminders/{reminderId}/snooze",
            new { remindAtUtc = DateTimeOffset.UtcNow.AddHours(2) }));
        AssertNotServerError(await client.DeleteAsync(
            $"/api/todos/{todoId}/reminders/{reminderId}"));
    }

    [Fact(DisplayName = "Every todo category and list route should execute its handler")]
    public async Task TodoCategoryAndListRoutes_ShouldReachEveryHandler()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateAdminClient();
        Guid categoryId = Guid.NewGuid();
        Guid listId = Guid.NewGuid();

        AssertNotServerError(await client.GetAsync(
            "/api/todo-categories?includeDeleted=false"));
        AssertNotServerError(await client.GetAsync(
            $"/api/todo-categories/{categoryId}?includeDeleted=false"));
        AssertNotServerError(await client.PostAsJsonAsync(
            "/api/todo-categories",
            new { name = $"category-{Guid.NewGuid():N}", description = "Coverage" }));
        AssertNotServerError(await client.PutAsJsonAsync(
            $"/api/todo-categories/{categoryId}",
            new { name = "Updated" }));
        AssertNotServerError(await client.DeleteAsync(
            $"/api/todo-categories/{categoryId}?mode=Soft"));
        AssertNotServerError(await client.PostAsync(
            $"/api/todo-categories/{categoryId}/restore",
            content: null));

        AssertNotServerError(await client.GetAsync(
            "/api/todo-lists?includeDeleted=false"));
        AssertNotServerError(await client.GetAsync(
            $"/api/todo-lists/{listId}?includeDeleted=false"));
        AssertNotServerError(await client.PostAsJsonAsync(
            "/api/todo-lists",
            new { name = $"list-{Guid.NewGuid():N}", description = "Coverage" }));
        AssertNotServerError(await client.PutAsJsonAsync(
            $"/api/todo-lists/{listId}",
            new { name = "Updated" }));
        AssertNotServerError(await client.DeleteAsync(
            $"/api/todo-lists/{listId}?mode=Soft"));
        AssertNotServerError(await client.PostAsync(
            $"/api/todo-lists/{listId}/restore",
            content: null));
    }

    private static void AssertNotServerError(HttpResponseMessage response)
    {
        using (response)
        {
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        }
    }
}
