using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Modules.Payments.Adapters.PayPal;

/// <summary>Caches PayPal OAuth tokens for the lifetime of the process.</summary>
public sealed class PayPalTokenProvider(HttpClient client, PayPalOptions options)
{
    /// <summary>The OAuth token endpoint.</summary>
    private const string TokenPath = "v1/oauth2/token";

    /// <summary>How early a token is renewed.</summary>
    private const int RenewalMarginMinutes = 1;

    private string? _token;
    private DateTimeOffset _expiresAt;

    /// <summary>Returns a valid bearer token.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token.</returns>
    public async Task<string> GetAsync(CancellationToken cancellationToken)
    {
        if (_token is not null && _expiresAt > DateTimeOffset.UtcNow.AddMinutes(RenewalMarginMinutes))
            return _token;

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenPath)
        {
            Content = new StringContent(
                "grant_type=client_credentials",
                Encoding.UTF8,
                "application/x-www-form-urlencoded")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}")));

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        TokenResponse? token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);

        _token = token?.AccessToken ?? throw new InvalidOperationException("PayPal returned no access token.");
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);

        return _token;
    }

    /// <summary>The OAuth token response body.</summary>
    /// <param name="AccessToken">The access token.</param>
    /// <param name="ExpiresIn">Seconds until expiry.</param>
    private sealed record TokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn);
}
