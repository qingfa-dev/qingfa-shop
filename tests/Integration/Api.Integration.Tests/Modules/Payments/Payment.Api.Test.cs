using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Api.Integration.Tests.BuildingBlocks.Api;

using AwesomeAssertions;

using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

namespace Api.Integration.Tests.Modules.Payments;

/// <summary>Exercises the customer and admin payment surface over a real database.</summary>
[Collection(ResultContractStackCollection.Name)]
public sealed class PaymentApiTest(ResultContractStackFixture fixture)
{
    [Fact]
    public async Task Create_WithoutIdempotencyKey_ShouldReturn400()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/payments",
            new
            {
                amount = new { amount = 10m, currency = "USD" },
                provider = "fake",
                externalReference = "cart-1",
                captureMode = "automatic"
            });

        AssertFailureCode(response, PaymentsOutcome.Codes.IdempotencyKeyRequired, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ThenRead_ShouldReturnTheSamePayment()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();

        (Guid id, HttpStatusCode status) = await CreatePaymentAsync(client, NewKey(), 10m, "cart-2");

        status.Should().Be(HttpStatusCode.Created);

        using HttpResponseMessage read = await client.GetAsync($"/api/payments/{id}");
        read.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument document = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("value").GetProperty("id").GetGuid().Should().Be(id);
    }

    [Fact]
    public async Task Read_OtherCustomersPayment_ShouldReturn404()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient owner = fixture.CreateClient();
        using HttpClient other = fixture.CreateClient();

        (Guid id, _) = await CreatePaymentAsync(owner, NewKey(), 10m, "cart-3");

        using HttpResponseMessage read = await other.GetAsync($"/api/payments/{id}");

        read.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_SameKey_ShouldReplayAndConflictOnDifferentBody()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();
        string key = NewKey();

        (Guid firstId, HttpStatusCode firstStatus) = await CreatePaymentAsync(client, key, 15m, "cart-idem");
        firstStatus.Should().Be(HttpStatusCode.Created);

        (Guid replayId, HttpStatusCode replayStatus) = await CreatePaymentAsync(client, key, 15m, "cart-idem");
        replayId.Should().Be(firstId);
        replayStatus.Should().Be(HttpStatusCode.OK);

        (_, HttpStatusCode conflict) = await CreatePaymentAsync(client, key, 16m, "cart-idem");
        conflict.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AdminRoute_WithoutPermission_ShouldReturn403()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient customer = fixture.CreateClient();
        (Guid id, _) = await CreatePaymentAsync(customer, NewKey(), 5m, "cart-perm");

        using HttpResponseMessage response = await customer.PostAsJsonAsync(
            $"/api/admin/payments/{id}/capture",
            new { amount = (object?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminRead_WithPermission_ShouldReturnThePayment()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient customer = fixture.CreateClient();
        using HttpClient admin = fixture.CreateClient("Admin");

        (Guid id, _) = await CreatePaymentAsync(customer, NewKey(), 12m, "cart-admin");

        using HttpResponseMessage read = await admin.GetAsync($"/api/admin/payments/{id}");

        read.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Creates a payment and returns its id and status.</summary>
    private static async Task<(Guid Id, HttpStatusCode Status)> CreatePaymentAsync(
        HttpClient client,
        string idempotencyKey,
        decimal amount,
        string externalReference)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(new
            {
                amount = new { amount, currency = "USD" },
                provider = "fake",
                externalReference,
                captureMode = "automatic"
            })
        };

        request.Headers.Add(PaymentConstant.Headers.IdempotencyKey, idempotencyKey);

        using HttpResponseMessage response = await client.SendAsync(request);

        if (response.StatusCode is not (HttpStatusCode.Created or HttpStatusCode.OK))
            return (Guid.Empty, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return (document.RootElement.GetProperty("value").GetProperty("id").GetGuid(), response.StatusCode);
    }

    /// <summary>Asserts a failure body carries the expected code.</summary>
    private static void AssertFailureCode(HttpResponseMessage response, string expected, HttpStatusCode status)
    {
        response.StatusCode.Should().Be(status);

        using JsonDocument document = JsonDocument.Parse(
            response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

        document.RootElement.GetProperty("errors")[0].GetProperty("code").GetString()
            .Should().Be(expected);
    }

    /// <summary>Creates a unique idempotency key.</summary>
    private static string NewKey() => Guid.NewGuid().ToString();

    [Fact]
    public async Task Create_WithZeroAmount_ShouldReturn400WithTheCanonicalCode()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(new
            {
                amount = new { amount = 0m, currency = "USD" },
                provider = "fake",
                externalReference = "cart-zero",
                captureMode = "automatic"
            })
        };
        request.Headers.Add(PaymentConstant.Headers.IdempotencyKey, NewKey());

        using HttpResponseMessage response = await client.SendAsync(request);

        AssertFailureCode(response, PaymentsOutcome.Codes.AmountInvalid, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithUnknownProvider_ShouldReturn400()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(new
            {
                amount = new { amount = 5m, currency = "USD" },
                provider = "not-a-provider",
                externalReference = "cart-unknown",
                captureMode = "automatic"
            })
        };
        request.Headers.Add(PaymentConstant.Headers.IdempotencyKey, NewKey());

        using HttpResponseMessage response = await client.SendAsync(request);

        AssertFailureCode(response, PaymentsOutcome.Codes.ProviderUnknown, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithoutToken_ShouldReturn401()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(new
            {
                amount = new { amount = 5m, currency = "USD" },
                provider = "fake",
                externalReference = "cart-anon",
                captureMode = "automatic"
            })
        };
        request.Headers.Add(PaymentConstant.Headers.IdempotencyKey, NewKey());

        using HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refund_WithoutReason_ShouldReturn400()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient customer = fixture.CreateClient();
        using HttpClient admin = fixture.CreateClient("Admin");

        (Guid id, _) = await CreatePaymentAsync(customer, NewKey(), 20m, "cart-reason");

        using var refund = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/payments/{id}/refunds")
        {
            Content = JsonContent.Create(new
            {
                amount = new { amount = 5m, currency = "USD" },
                reason = "   "
            })
        };
        refund.Headers.Add(PaymentConstant.Headers.IdempotencyKey, NewKey());

        using HttpResponseMessage response = await admin.SendAsync(refund);

        AssertFailureCode(response, PaymentsOutcome.Codes.RefundReasonRequired, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_AboveTheCeiling_ShouldReturn400()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(new
            {
                amount = new { amount = PaymentConstant.Defaults.MaxAmount + 1m, currency = "USD" },
                provider = "fake",
                externalReference = "cart-ceiling",
                captureMode = "automatic"
            })
        };
        request.Headers.Add(PaymentConstant.Headers.IdempotencyKey, NewKey());

        using HttpResponseMessage response = await client.SendAsync(request);

        AssertFailureCode(response, PaymentsOutcome.Codes.AmountExceedsLimit, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_WithAnOversizedPage_ShouldStillReturn200()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/payments?page=1&pageSize={PaymentConstant.Constraints.MaxPageSize + 500}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
