using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Unit.Tests.Integrations.Storages.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Storage")]
public sealed class StorageServiceTests
{
    private readonly Mock<IStorageProvider> _providerMock;
    private readonly Mock<IStorageSecurityEnforcer> _enforcerMock;
    private readonly Mock<IStorageAntiForgeryGuard> _antiforgeryGuardMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<StorageService>> _loggerMock;
    private readonly Dictionary<string, IStorageProvider> _providers;
    private const string DefaultProviderName = "default";

    public StorageServiceTests()
    {
        _providerMock = new Mock<IStorageProvider>();
        _providerMock.Setup(x => x.Name).Returns("default");
        _providers = new Dictionary<string, IStorageProvider>
        {
            [DefaultProviderName] = _providerMock.Object
        };

        _enforcerMock = new Mock<IStorageSecurityEnforcer>();
        _antiforgeryGuardMock = new Mock<IStorageAntiForgeryGuard>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<StorageService>>();
    }

    private StorageService CreateSut(
        IStorageMalwareScanner? malwareScanner = null,
        IImageProcessor? imageProcessor = null,
        string? encryptionKey = null)
    {
        IOptions<StorageSecuritySetting>? securityOptions = null;
        if (encryptionKey is not null)
        {
            var optionsMock = new Mock<IOptions<StorageSecuritySetting>>();
            optionsMock.Setup(x => x.Value).Returns(new StorageSecuritySetting { EncryptionKey = encryptionKey });
            securityOptions = optionsMock.Object;
        }

        return new StorageService(
            _providers,
            DefaultProviderName,
            _enforcerMock.Object,
            _antiforgeryGuardMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object,
            malwareScanner,
            imageProcessor,
            securityOptions);
    }

    private static UploadRequest CreateValidRequest()
    {
        return new UploadRequest
        {
            Key = "test/file.txt",
            Content = new MemoryStream("hello"u8.ToArray()),
            ContentType = "text/plain"
        };
    }

    [Fact(DisplayName = "UploadAsync with unknown provider should return ProviderNotFound")]
    public async Task UploadAsync_WithUnknownProvider_ShouldReturnProviderNotFound()
    {
        StorageService sut = CreateSut();

        Result<UploadResult> result = await sut.UploadAsync(
            CreateValidRequest(),
            "nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.ProviderNotFound");
    }

    [Fact(DisplayName = "UploadAsync with default provider and null providerName should succeed")]
    public async Task UploadAsync_WithDefaultProviderAndNullProviderName_ShouldSucceed()
    {
        StorageService sut = CreateSut();
        UploadResult expectedResult = new()
        {
            Key = "test/file.txt",
            Provider = "default",
            Uri = null,
            SizeBytes = 5,
            StoredAtUtc = DateTimeOffset.UtcNow
        };

        _providerMock.Setup(x => x.UploadAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadResult>.Ok(expectedResult));
        _enforcerMock.Setup(x => x.EnforceAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result<UploadResult> result = await sut.UploadAsync(CreateValidRequest(), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedResult);
    }

    [Fact(DisplayName = "UploadAsync with invalid CSRF token should return AccessDenied")]
    public async Task UploadAsync_WithInvalidCsrfToken_ShouldReturnAccessDenied()
    {
        StorageService sut = CreateSut();
        DefaultHttpContext httpContext = new();

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _antiforgeryGuardMock
            .Setup(x => x.ValidateRequestAsync("anonymous", httpContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StorageResult.Failure.AccessDenied("test/file.txt"));

        Result<UploadResult> result = await sut.UploadAsync(CreateValidRequest());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.AccessDenied");
    }

    [Fact(DisplayName = "UploadAsync with valid CSRF token should proceed to security check")]
    public async Task UploadAsync_WithValidCsrfToken_ShouldProceedToSecurityCheck()
    {
        StorageService sut = CreateSut();
        DefaultHttpContext httpContext = new();

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _antiforgeryGuardMock
            .Setup(x => x.ValidateRequestAsync("anonymous", httpContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _enforcerMock.Setup(x => x.EnforceAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _providerMock.Setup(x => x.UploadAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadResult>.Ok(new UploadResult { Key = "key", Provider = "default", Uri = null, SizeBytes = 0, StoredAtUtc = DateTimeOffset.UtcNow }));

        Result<UploadResult> result = await sut.UploadAsync(CreateValidRequest());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "UploadAsync with null HttpContext should skip CSRF check")]
    public async Task UploadAsync_WithNullHttpContext_ShouldSkipCsrfCheck()
    {
        StorageService sut = CreateSut();

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        _enforcerMock.Setup(x => x.EnforceAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _providerMock.Setup(x => x.UploadAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadResult>.Ok(new UploadResult { Key = "key", Provider = "default", Uri = null, SizeBytes = 0, StoredAtUtc = DateTimeOffset.UtcNow }));

        Result<UploadResult> result = await sut.UploadAsync(CreateValidRequest());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "UploadAsync when security check fails should return failure")]
    public async Task UploadAsync_WhenSecurityCheckFails_ShouldReturnFailure()
    {
        StorageService sut = CreateSut();
        Error securityError = Error.Validation("Storage.FileTooLarge", "File exceeds the maximum allowed size of 1048576 bytes.");

        _enforcerMock.Setup(x => x.EnforceAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result)securityError);

        Result<UploadResult> result = await sut.UploadAsync(CreateValidRequest());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.FileTooLarge");
    }

    [Fact(DisplayName = "UploadAsync when provider fails should return failure")]
    public async Task UploadAsync_WhenProviderFails_ShouldReturnFailure()
    {
        StorageService sut = CreateSut();
        Error providerError = Error.Unexpected("Storage.ProviderError", "Provider error: connection refused");

        _enforcerMock.Setup(x => x.EnforceAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _providerMock.Setup(x => x.UploadAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result<UploadResult>)providerError);

        Result<UploadResult> result = await sut.UploadAsync(CreateValidRequest());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.ProviderError");
    }

    [Fact(DisplayName = "UploadAsync with malware scan rejecting threat should return MalwareRejected")]
    public async Task UploadAsync_WithMalwareRejection_ShouldReturnMalwareRejected()
    {
        var malwareScannerMock = new Mock<IStorageMalwareScanner>();
        malwareScannerMock
            .Setup(x => x.ScanAsync(It.IsAny<Stream>(), It.IsAny<string?>(), It.IsAny<UploadOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MalwareScannerResult.ScanSucceeded(new MalwareScanResult
            {
                IsClean = false,
                ThreatName = "Win.Test.EICAR",
                ScanEngine = "Test"
            }));

        StorageService sut = CreateSut(malwareScanner: malwareScannerMock.Object);
        _enforcerMock.Setup(x => x.EnforceAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var request = CreateValidRequest();
        var opts = new UploadOptions { ScanForMalware = true };

        Result<UploadResult> result = await sut.UploadAsync(request, options: opts);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.MalwareRejected");
    }

    [Fact(DisplayName = "UploadAsync with clean malware scan should proceed with upload")]
    public async Task UploadAsync_WithCleanMalwareScan_ShouldProceedToUpload()
    {
        var malwareScannerMock = new Mock<IStorageMalwareScanner>();
        malwareScannerMock
            .Setup(x => x.ScanAsync(It.IsAny<Stream>(), It.IsAny<string?>(), It.IsAny<UploadOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MalwareScannerResult.ScanSucceeded(new MalwareScanResult { IsClean = true, ScanEngine = "Test" }));

        StorageService sut = CreateSut(malwareScanner: malwareScannerMock.Object);
        _enforcerMock.Setup(x => x.EnforceAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _providerMock.Setup(x => x.UploadAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadResult>.Ok(new UploadResult { Key = "key", Provider = "default", Uri = null, SizeBytes = 0, StoredAtUtc = DateTimeOffset.UtcNow }));

        var request = CreateValidRequest();
        var opts = new UploadOptions { ScanForMalware = true };

        Result<UploadResult> result = await sut.UploadAsync(request, options: opts);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "UploadAsync with malware scan failure should return failure")]
    public async Task UploadAsync_WithMalwareScanFailure_ShouldReturnFailure()
    {
        var malwareScannerMock = new Mock<IStorageMalwareScanner>();
        malwareScannerMock
            .Setup(x => x.ScanAsync(It.IsAny<Stream>(), It.IsAny<string?>(), It.IsAny<UploadOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MalwareScanResult>.Fail(MalwareScannerResult.Failure.ScanFailed("test", "connection error")));

        StorageService sut = CreateSut(malwareScanner: malwareScannerMock.Object);
        _enforcerMock.Setup(x => x.EnforceAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var request = CreateValidRequest();
        var opts = new UploadOptions { ScanForMalware = true };

        Result<UploadResult> result = await sut.UploadAsync(request, options: opts);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.ScanFailed");
    }

    [Fact(DisplayName = "UploadAsync with image processing should invoke processor")]
    public async Task UploadAsync_WithImageProcessing_ShouldInvokeProcessor()
    {
        var imageProcessorMock = new Mock<IImageProcessor>();
        var processedStream = new MemoryStream("processed"u8.ToArray());
        imageProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<Stream>(), It.IsAny<UploadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Stream>.Ok(processedStream));

        StorageService sut = CreateSut(imageProcessor: imageProcessorMock.Object);
        _enforcerMock.Setup(x => x.EnforceAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _providerMock.Setup(x => x.UploadAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadResult>.Ok(new UploadResult { Key = "key", Provider = "default", Uri = null, SizeBytes = 0, StoredAtUtc = DateTimeOffset.UtcNow }));

        var request = CreateValidRequest();
        var opts = new UploadOptions { ResizeWidth = 100, ResizeHeight = 100 };

        Result<UploadResult> result = await sut.UploadAsync(request, options: opts);

        result.IsSuccess.Should().BeTrue();
        imageProcessorMock.Verify(x => x.ProcessAsync(It.IsAny<Stream>(), It.IsAny<UploadOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "UploadAsync without UploadOptions should use defaults and skip pipeline")]
    public async Task UploadAsync_WithoutOptions_ShouldSkipPipeline()
    {
        var malwareScannerMock = new Mock<IStorageMalwareScanner>(MockBehavior.Strict);

        StorageService sut = CreateSut(malwareScanner: malwareScannerMock.Object);
        _enforcerMock.Setup(x => x.EnforceAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _providerMock.Setup(x => x.UploadAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadResult>.Ok(new UploadResult { Key = "key", Provider = "default", Uri = null, SizeBytes = 0, StoredAtUtc = DateTimeOffset.UtcNow }));

        Result<UploadResult> result = await sut.UploadAsync(CreateValidRequest());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "DownloadAsync with unknown provider should return ProviderNotFound")]
    public async Task DownloadAsync_WithUnknownProvider_ShouldReturnProviderNotFound()
    {
        StorageService sut = CreateSut();

        Result<DownloadResult> result = await sut.DownloadAsync("key", "nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.ProviderNotFound");
    }

    [Fact(DisplayName = "DownloadAsync with valid provider should succeed")]
    public async Task DownloadAsync_WithValidProvider_ShouldSucceed()
    {
        StorageService sut = CreateSut();
        DownloadResult expectedResult = new()
        {
            Content = new MemoryStream(),
            Info = new StoredObjectInfo
            {
                Key = "key",
                Provider = "default",
                SizeBytes = 0,
                LastModifiedUtc = DateTimeOffset.UtcNow,
                ContentType = null
            }
        };

        _providerMock.Setup(x => x.DownloadAsync("key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DownloadResult>.Ok(expectedResult));

        Result<DownloadResult> result = await sut.DownloadAsync("key");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedResult);
    }

    [Fact(DisplayName = "DownloadAsync when provider fails should return failure")]
    public async Task DownloadAsync_WhenProviderFails_ShouldReturnFailure()
    {
        StorageService sut = CreateSut();
        Error notFoundError = Error.NotFound("Storage.NotFound", "Object 'key' was not found.");

        _providerMock.Setup(x => x.DownloadAsync("key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result<DownloadResult>)notFoundError);

        Result<DownloadResult> result = await sut.DownloadAsync("key");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.NotFound");
    }

    [Fact(DisplayName = "DeleteAsync with unknown provider should return ProviderNotFound")]
    public async Task DeleteAsync_WithUnknownProvider_ShouldReturnProviderNotFound()
    {
        StorageService sut = CreateSut();

        Result result = await sut.DeleteAsync("key", "nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.ProviderNotFound");
    }

    [Fact(DisplayName = "DeleteAsync with valid provider should succeed")]
    public async Task DeleteAsync_WithValidProvider_ShouldSucceed()
    {
        StorageService sut = CreateSut();

        _providerMock.Setup(x => x.DeleteAsync("key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await sut.DeleteAsync("key");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "DeleteAsync when provider fails should return failure")]
    public async Task DeleteAsync_WhenProviderFails_ShouldReturnFailure()
    {
        StorageService sut = CreateSut();

        _providerMock.Setup(x => x.DeleteAsync("key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(Error.NotFound("Storage.NotFound", "Object not found")));

        Result result = await sut.DeleteAsync("key");

        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "StatAsync with unknown provider should return ProviderNotFound")]
    public async Task StatAsync_WithUnknownProvider_ShouldReturnProviderNotFound()
    {
        StorageService sut = CreateSut();

        Result<StoredObjectInfo> result = await sut.StatAsync("key", "nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.ProviderNotFound");
    }

    [Fact(DisplayName = "StatAsync with valid provider should succeed")]
    public async Task StatAsync_WithValidProvider_ShouldSucceed()
    {
        StorageService sut = CreateSut();
        StoredObjectInfo expectedInfo = new() { Key = "key", Provider = "default", SizeBytes = 100, LastModifiedUtc = DateTimeOffset.UtcNow, ContentType = "text/plain" };

        _providerMock.Setup(x => x.StatAsync("key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StoredObjectInfo>.Ok(expectedInfo));

        Result<StoredObjectInfo> result = await sut.StatAsync("key");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedInfo);
    }

    [Fact(DisplayName = "ListAsync with unknown provider should return ProviderNotFound")]
    public async Task ListAsync_WithUnknownProvider_ShouldReturnProviderNotFound()
    {
        StorageService sut = CreateSut();

        Result<IReadOnlyList<StoredObjectInfo>> result = await sut.ListAsync(providerName: "nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.ProviderNotFound");
    }

    [Fact(DisplayName = "ListAsync with valid provider should succeed")]
    public async Task ListAsync_WithValidProvider_ShouldSucceed()
    {
        StorageService sut = CreateSut();
        List<StoredObjectInfo> expectedList = new()
        {
            new StoredObjectInfo { Key = "file1.txt", Provider = "default", SizeBytes = 10, LastModifiedUtc = DateTimeOffset.UtcNow, ContentType = "text/plain" }
        };

        _providerMock.Setup(x => x.ListAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<StoredObjectInfo>>.Ok(expectedList));

        Result<IReadOnlyList<StoredObjectInfo>> result = await sut.ListAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedList);
    }

    [Fact(DisplayName = "UploadAsync with GenerateHash should add content-hash metadata")]
    public async Task UploadAsync_WithGenerateHash_ShouldAddContentHash()
    {
        StorageService sut = CreateSut();
        _enforcerMock.Setup(x => x.EnforceAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        UploadRequest? capturedRequest = null;
        _providerMock.Setup(x => x.UploadAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .Callback<UploadRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(Result<UploadResult>.Ok(new UploadResult { Key = "key", Provider = "default", Uri = null, SizeBytes = 0, StoredAtUtc = DateTimeOffset.UtcNow }));

        var request = CreateValidRequest();
        var opts = new UploadOptions { GenerateHash = true };

        Result<UploadResult> result = await sut.UploadAsync(request, options: opts);

        result.IsSuccess.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Metadata.Should().ContainKey("content-hash");
    }

    [Fact(DisplayName = "UploadAsync with Encrypt should transform stream")]
    public async Task UploadAsync_WithEncrypt_ShouldTransformStream()
    {
        StorageService sut = CreateSut(encryptionKey: "ThisIsA32ByteKeyForAES256!!!");
        _enforcerMock.Setup(x => x.EnforceAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        UploadRequest? capturedRequest = null;
        _providerMock.Setup(x => x.UploadAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .Callback<UploadRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(Result<UploadResult>.Ok(new UploadResult { Key = "key", Provider = "default", Uri = null, SizeBytes = 0, StoredAtUtc = DateTimeOffset.UtcNow }));

        var request = CreateValidRequest();
        var opts = new UploadOptions { Encrypt = true };

        Result<UploadResult> result = await sut.UploadAsync(request, options: opts);

        result.IsSuccess.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Content.Should().NotBeSameAs(request.Content);
        capturedRequest.Metadata.Should().ContainKey("encrypted");
        capturedRequest.Metadata!["encrypted"].Should().Be("true");
    }

    [Fact(DisplayName = "UploadAsync with Overwrite should add overwrite-existing metadata")]
    public async Task UploadAsync_WithOverwrite_ShouldAddOverwriteMetadata()
    {
        StorageService sut = CreateSut();
        _enforcerMock.Setup(x => x.EnforceAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        UploadRequest? capturedRequest = null;
        _providerMock.Setup(x => x.UploadAsync(It.IsAny<UploadRequest>(), It.IsAny<CancellationToken>()))
            .Callback<UploadRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(Result<UploadResult>.Ok(new UploadResult { Key = "key", Provider = "default", Uri = null, SizeBytes = 0, StoredAtUtc = DateTimeOffset.UtcNow }));

        var request = CreateValidRequest();
        var opts = new UploadOptions { Overwrite = true };

        Result<UploadResult> result = await sut.UploadAsync(request, options: opts);

        result.IsSuccess.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Metadata.Should().ContainKey("overwrite-existing");
        capturedRequest.Metadata!["overwrite-existing"].Should().Be("true");
    }
}
