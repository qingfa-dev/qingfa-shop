using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Application.BuildingBlocks.Caching;

using StorageAntiForgeryGuard = Infrastructure.BuildingBlocks.Adapters.Storage.Security.Guard.StorageAntiForgeryGuard;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Storage.Security;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Storage")]
public sealed class StorageAntiForgeryGuardTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IAntiforgery> _antiforgeryMock;
    private readonly Mock<IOptions<AntiForgeryOptions>> _optionsMock;
    private readonly Mock<ILogger<StorageAntiForgeryGuard>> _loggerMock;
    private readonly Dictionary<string, int> _cacheStore = new();

    public StorageAntiForgeryGuardTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _antiforgeryMock = new Mock<IAntiforgery>();
        _optionsMock = new Mock<IOptions<AntiForgeryOptions>>();
        _loggerMock = new Mock<ILogger<StorageAntiForgeryGuard>>();

        _optionsMock.Setup(x => x.Value).Returns(new AntiForgeryOptions());

        _cacheServiceMock
            .Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, ValueTask<int>>>(),
                It.IsAny<CachingEntryOption?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .Returns((string key, Func<CancellationToken, ValueTask<int>> _, CachingEntryOption? __, IEnumerable<string>? ___, CancellationToken ____) =>
            {
                if (!_cacheStore.TryGetValue(key, out int value))
                    value = 0;
                return ValueTask.FromResult(Result<int>.Ok(value));
            });

        _cacheServiceMock
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CachingEntryOption?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback((string key, int value, CachingEntryOption? _, IEnumerable<string>? __, CancellationToken ___) =>
            {
                _cacheStore[key] = value;
            })
            .Returns(ValueTask.FromResult(Result.Ok()));

        _cacheServiceMock
            .Setup(x => x.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback((string key, CancellationToken _) =>
            {
                _cacheStore.Remove(key);
            })
            .Returns(ValueTask.FromResult(Result.Ok()));
    }

    private StorageAntiForgeryGuard CreateSut()
        => new(_cacheServiceMock.Object, _antiforgeryMock.Object, _optionsMock.Object, _loggerMock.Object);

    [Fact(DisplayName = "RecordFailureAsync within threshold should return Ok")]
    public async Task RecordFailureAsync_WithinThreshold_ShouldReturnOk()
    {
        StorageAntiForgeryGuard sut = CreateSut();
        int threshold = AntiForgeryOptionsConstant.Defaults.MaxConsecutiveFailures;

        for (int i = 0; i < threshold - 1; i++)
        {
            Result result = await sut.RecordFailureAsync("user-1");
            result.IsSuccess.Should().BeTrue($"failure {i + 1} should succeed");
        }
    }

    [Fact(DisplayName = "RecordFailureAsync when threshold reached should return TooManyAttempts")]
    public async Task RecordFailureAsync_WhenThresholdReached_ShouldReturnTooManyAttempts()
    {
        StorageAntiForgeryGuard sut = CreateSut();
        int threshold = AntiForgeryOptionsConstant.Defaults.MaxConsecutiveFailures;

        Result result = Result.Ok();
        for (int i = 0; i < threshold; i++)
            result = await sut.RecordFailureAsync("user-2");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.TooManyAttempts");
    }

    [Fact(DisplayName = "RecordFailureAsync when exceeding threshold should still return TooManyAttempts")]
    public async Task RecordFailureAsync_WhenExceedingThreshold_ShouldStillReturnTooManyAttempts()
    {
        StorageAntiForgeryGuard sut = CreateSut();
        int threshold = AntiForgeryOptionsConstant.Defaults.MaxConsecutiveFailures;

        for (int i = 0; i < threshold + 2; i++)
        {
            Result result = await sut.RecordFailureAsync("user-3");
            if (i >= threshold - 1)
            {
                result.IsFailure.Should().BeTrue($"failure {i + 1} should be blocked");
                result.Errors.Should().Contain(e => e.Code == "Storage.TooManyAttempts");
            }
        }
    }

    [Fact(DisplayName = "ResetAsync should clear the failure counter")]
    public async Task ResetAsync_ShouldClearCounter()
    {
        StorageAntiForgeryGuard sut = CreateSut();
        int threshold = AntiForgeryOptionsConstant.Defaults.MaxConsecutiveFailures;

        for (int i = 0; i < threshold; i++)
            await sut.RecordFailureAsync("user-4");

        await sut.ResetAsync("user-4");

        Result result = await sut.RecordFailureAsync("user-4");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "IsBlockedAsync when under threshold should return false")]
    public async Task IsBlockedAsync_WhenUnderThreshold_ShouldReturnFalse()
    {
        StorageAntiForgeryGuard sut = CreateSut();
        int threshold = AntiForgeryOptionsConstant.Defaults.MaxConsecutiveFailures;

        for (int i = 0; i < threshold - 2; i++)
            await sut.RecordFailureAsync("user-5");

        bool blocked = await sut.IsBlockedAsync("user-5");

        blocked.Should().BeFalse();
    }

    [Fact(DisplayName = "IsBlockedAsync when at threshold should return true")]
    public async Task IsBlockedAsync_WhenAtThreshold_ShouldReturnTrue()
    {
        StorageAntiForgeryGuard sut = CreateSut();
        int threshold = AntiForgeryOptionsConstant.Defaults.MaxConsecutiveFailures;

        for (int i = 0; i < threshold; i++)
            await sut.RecordFailureAsync("user-6");

        bool blocked = await sut.IsBlockedAsync("user-6");

        blocked.Should().BeTrue();
    }

    [Fact(DisplayName = "IsBlockedAsync when exceeding threshold should return true")]
    public async Task IsBlockedAsync_WhenExceedingThreshold_ShouldReturnTrue()
    {
        StorageAntiForgeryGuard sut = CreateSut();
        int threshold = AntiForgeryOptionsConstant.Defaults.MaxConsecutiveFailures;

        for (int i = 0; i < threshold + 3; i++)
            await sut.RecordFailureAsync("user-7");

        bool blocked = await sut.IsBlockedAsync("user-7");

        blocked.Should().BeTrue();
    }

    [Fact(DisplayName = "IsBlockedAsync after reset should return false")]
    public async Task IsBlockedAsync_AfterReset_ShouldReturnFalse()
    {
        StorageAntiForgeryGuard sut = CreateSut();
        int threshold = AntiForgeryOptionsConstant.Defaults.MaxConsecutiveFailures;

        for (int i = 0; i < threshold; i++)
            await sut.RecordFailureAsync("user-8");

        await sut.ResetAsync("user-8");

        bool blocked = await sut.IsBlockedAsync("user-8");

        blocked.Should().BeFalse();
    }

    [Fact(DisplayName = "IsBlockedAsync with no failures should return false")]
    public async Task IsBlockedAsync_WithNoFailures_ShouldReturnFalse()
    {
        StorageAntiForgeryGuard sut = CreateSut();

        bool blocked = await sut.IsBlockedAsync("unknown-user");

        blocked.Should().BeFalse();
    }

    [Fact(DisplayName = "RecordFailureAsync identity keys are independent")]
    public async Task RecordFailureAsync_IdentityKeysAreIndependent()
    {
        StorageAntiForgeryGuard sut = CreateSut();
        int threshold = AntiForgeryOptionsConstant.Defaults.MaxConsecutiveFailures;

        for (int i = 0; i < threshold; i++)
            await sut.RecordFailureAsync("key-A");

        await sut.RecordFailureAsync("key-B");
        bool blockedA = await sut.IsBlockedAsync("key-A");
        bool blockedB = await sut.IsBlockedAsync("key-B");

        blockedA.Should().BeTrue();
        blockedB.Should().BeFalse();
    }

    [Fact(DisplayName = "ValidateRequestAsync when already blocked should return TooManyAttempts without checking token")]
    public async Task ValidateRequestAsync_WhenAlreadyBlocked_ShouldReturnTooManyAttempts()
    {
        StorageAntiForgeryGuard sut = CreateSut();
        DefaultHttpContext httpContext = new();
        int threshold = AntiForgeryOptionsConstant.Defaults.MaxConsecutiveFailures;

        _cacheStore[$"antiforgery:failures:blocked-user"] = threshold;

        Result result = await sut.ValidateRequestAsync("blocked-user", httpContext);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.TooManyAttempts");
        _antiforgeryMock.Verify(x => x.IsRequestValidAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact(DisplayName = "ValidateRequestAsync when token invalid should record failure and return Ok if under threshold")]
    public async Task ValidateRequestAsync_WhenTokenInvalid_ShouldRecordFailure()
    {
        StorageAntiForgeryGuard sut = CreateSut();
        DefaultHttpContext httpContext = new();
        int threshold = AntiForgeryOptionsConstant.Defaults.MaxConsecutiveFailures;

        _cacheStore[$"antiforgery:failures:fraud-user"] = threshold - 2;
        _antiforgeryMock.Setup(x => x.IsRequestValidAsync(httpContext)).ReturnsAsync(false);

        Result result = await sut.ValidateRequestAsync("fraud-user", httpContext);

        result.IsSuccess.Should().BeTrue();
        _cacheStore[$"antiforgery:failures:fraud-user"].Should().Be(threshold - 1);
    }

    [Fact(DisplayName = "ValidateRequestAsync when token invalid and threshold reached should return TooManyAttempts")]
    public async Task ValidateRequestAsync_WhenTokenInvalidAndThresholdReached_ShouldReturnTooManyAttempts()
    {
        StorageAntiForgeryGuard sut = CreateSut();
        DefaultHttpContext httpContext = new();
        int threshold = AntiForgeryOptionsConstant.Defaults.MaxConsecutiveFailures;

        _cacheStore[$"antiforgery:failures:edge-user"] = threshold - 1;
        _antiforgeryMock.Setup(x => x.IsRequestValidAsync(httpContext)).ReturnsAsync(false);

        Result result = await sut.ValidateRequestAsync("edge-user", httpContext);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.TooManyAttempts");
    }

    [Fact(DisplayName = "ValidateRequestAsync when token valid should reset counter and return Ok")]
    public async Task ValidateRequestAsync_WhenTokenValid_ShouldResetAndReturnOk()
    {
        StorageAntiForgeryGuard sut = CreateSut();
        DefaultHttpContext httpContext = new();

        _cacheStore[$"antiforgery:failures:valid-user"] = 3;
        _antiforgeryMock.Setup(x => x.IsRequestValidAsync(httpContext)).ReturnsAsync(true);

        Result result = await sut.ValidateRequestAsync("valid-user", httpContext);

        result.IsSuccess.Should().BeTrue();
        _cacheStore.ContainsKey($"antiforgery:failures:valid-user").Should().BeFalse();
    }

    [Fact(DisplayName = "ValidateRequestAsync when no prior failures and token valid should return Ok")]
    public async Task ValidateRequestAsync_WhenNoPriorFailuresAndTokenValid_ShouldReturnOk()
    {
        StorageAntiForgeryGuard sut = CreateSut();
        DefaultHttpContext httpContext = new();

        _antiforgeryMock.Setup(x => x.IsRequestValidAsync(httpContext)).ReturnsAsync(true);

        Result result = await sut.ValidateRequestAsync("fresh-user", httpContext);

        result.IsSuccess.Should().BeTrue();
    }
}
