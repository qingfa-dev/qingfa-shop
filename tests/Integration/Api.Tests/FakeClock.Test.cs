using System.Net.Http.Json;

using Testing;
using Testing.Fixtures;

namespace Api.Tests;

/// <summary>
/// Proves <c>/time</c> reads the injected <c>TimeProvider</c>, not the system clock.
/// </summary>
/// <remarks>
/// The suite shares one <see cref="ShopApiFactory"/> through
/// <see cref="ApiHostCollection"/>, and <c>FakeTimeProvider</c> rejects backward moves,
/// so every test advances from the clock's current instant instead of setting an absolute
/// date. That keeps the result independent of execution order.
/// </remarks>
[Collection(ApiHostCollection.Name)]
public sealed class FakeClockTest : IntegrationTestBase
{
    public FakeClockTest(ShopApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetTime_ReturnsFakedClock()
    {
        // Arrange: a fixed instant no system clock would produce.
        var instant = Factory.Clock.GetUtcNow().AddDays(1);
        Factory.Clock.SetUtcNow(instant);

        // Act
        using var response = await Client.GetAsync("/time");

        // Assert: exact equality. A tolerance window would let the real clock pass.
        var payload = await response.Content.ReadFromJsonAsync<TimeResponse>();
        Assert.NotNull(payload);
        Assert.Equal(instant, payload.UtcNow);
    }

    [Fact]
    public async Task GetTime_IsNotSystemClock()
    {
        // Arrange: a decade ahead of the fake clock's current instant, which no system
        // clock can report.
        var instant = Factory.Clock.GetUtcNow().AddYears(10);
        Factory.Clock.SetUtcNow(instant);

        // Act
        using var response = await Client.GetAsync("/time");

        // Assert
        var payload = await response.Content.ReadFromJsonAsync<TimeResponse>();
        Assert.NotNull(payload);

        var skew = (payload.UtcNow - DateTimeOffset.UtcNow).Duration();
        Assert.True(skew > TimeSpan.FromDays(180),
            $"Expected a clock a decade off, but the response was only {skew} from now.");
    }

    private sealed record TimeResponse(DateTimeOffset UtcNow);
}