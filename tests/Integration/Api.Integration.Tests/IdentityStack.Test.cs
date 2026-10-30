using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Integration.Tests;

/// <summary>Shared helpers for the container-backed identity route suites.</summary>
/// <remarks>
/// Every suite runs against one migrated PostgreSQL host and the real Identity stores.
/// The recording email sender stands in only for the outbound transport, mirroring the
/// production <c>IIdentityEmailSender</c> contract without sending mail.
/// </remarks>
public abstract class IdentityStackTestBase
{
    protected IdentityStackTestBase(ResultContractStackFixture fixture)
    {
        Fixture = fixture;
        Fixture.Factory?.Email.Clear();
    }

    /// <summary>The container-backed host shared by the identity suites.</summary>
    protected ResultContractStackFixture Fixture { get; }

    /// <summary>Creates a client carrying the admin role, which bypasses permission checks.</summary>
    protected HttpClient CreateAdminClient() => Fixture.CreateClient("Admin");

    /// <summary>Creates a client carrying the supplied roles.</summary>
    protected HttpClient CreateClient(params string[] roles) => Fixture.CreateClient(roles);

    /// <summary>Creates an anonymous client that sends the supplied bearer token.</summary>
    protected HttpClient CreateBearerClient(string accessToken)
    {
        HttpClient client = Fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>Skips the current test when no container engine is available.</summary>
    protected void SkipWithoutContainer()
    {
        if (Fixture.SkipReason is { } reason)
            Assert.Skip(reason);
    }

    /// <summary>Reads a response body as JSON.</summary>
    protected static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        return JsonDocument.Parse(json);
    }

    /// <summary>Reads the <c>value</c> property of a successful Result body.</summary>
    protected static async Task<JsonElement> ReadValueAsync(HttpResponseMessage response)
    {
        using JsonDocument document = await ReadJsonAsync(response);
        return document.RootElement.GetProperty("value").Clone();
    }

    /// <summary>Asserts the shared Result failure contract and returns the first error code.</summary>
    protected static async Task<string> AssertFailureAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        response.StatusCode.Should().Be(expectedStatus);

        using JsonDocument document = await ReadJsonAsync(response);

        document.RootElement.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        document.RootElement.GetProperty("errors").GetArrayLength().Should().BePositive();
        document.RootElement.TryGetProperty("value", out _).Should().BeFalse();

        return document.RootElement.GetProperty("errors")[0].GetProperty("code").GetString()!;
    }

    /// <summary>Asserts a failure with a stable error code.</summary>
    protected static async Task AssertFailureCodeAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        string actual = await AssertFailureAsync(response, expectedStatus);
        actual.Should().Be(expectedCode);
    }

    /// <summary>Asserts a 204 response with an empty body.</summary>
    protected static async Task AssertNoContentAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        string body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        body.Should().BeEmpty();
    }

    /// <summary>Extracts a query-string value from an identity email link.</summary>
    protected static string ReadLinkQuery(string link, string name)
    {
        int query = link.IndexOf('?', StringComparison.Ordinal);
        query.Should().BeGreaterThanOrEqualTo(0);

        foreach (string pair in link[(query + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            if (parts.Length == 2 && string.Equals(parts[0], name, StringComparison.Ordinal))
                return Uri.UnescapeDataString(parts[1]);
        }

        throw new InvalidOperationException(
            $"The link does not contain a '{name}' query parameter: {link}");
    }

    /// <summary>Logs in through the public route and returns the token pair value.</summary>
    protected static async Task<JsonElement> LoginAsync(
        HttpClient client,
        string identifier,
        string password)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { identifier, password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await ReadValueAsync(response);
    }

    /// <summary>Finds a seeded role identifier by name.</summary>
    protected static async Task<Guid> FindRoleIdAsync(HttpClient client, string name)
    {
        using HttpResponseMessage response = await client.GetAsync(
            $"/api/identity/roles?search={Uri.EscapeDataString(name)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement value = await ReadValueAsync(response);
        foreach (JsonElement item in value.GetProperty("items").EnumerateArray())
        {
            if (string.Equals(
                item.GetProperty("name").GetString(),
                name,
                StringComparison.OrdinalIgnoreCase))
            {
                return item.GetProperty("id").GetGuid();
            }
        }

        throw new InvalidOperationException($"The seeded role '{name}' was not found.");
    }

    /// <summary>Finds a seeded user identifier by name.</summary>
    protected static async Task<Guid> FindUserIdAsync(HttpClient client, string name)
    {
        using HttpResponseMessage response = await client.GetAsync(
            $"/api/identity/users?search={Uri.EscapeDataString(name)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement value = await ReadValueAsync(response);
        foreach (JsonElement item in value.GetProperty("items").EnumerateArray())
        {
            if (string.Equals(
                item.GetProperty("userName").GetString(),
                name,
                StringComparison.Ordinal))
            {
                return item.GetProperty("id").GetGuid();
            }
        }

        throw new InvalidOperationException($"The seeded user '{name}' was not found.");
    }

    /// <summary>Builds a unique username within the user-domain 32-character limit.</summary>
    protected static string UniqueUserName() => $"u{Guid.NewGuid():N}"[..31];

    /// <summary>Builds a unique email address for a generated username.</summary>
    protected static string UniqueEmail(string userName) => $"{userName}@example.com";
}
