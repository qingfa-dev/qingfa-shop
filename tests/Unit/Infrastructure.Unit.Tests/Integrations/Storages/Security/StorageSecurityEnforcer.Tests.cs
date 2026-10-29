using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Unit.Tests.Integrations.Storages.Security;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Storage")]
public sealed class StorageSecurityEnforcerTests
{
    private readonly Mock<IOptions<StorageSecuritySetting>> _optionsMock;
    private readonly Mock<ILogger<StorageSecurityEnforcer>> _loggerMock;

    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] InvalidHeader = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
    private static readonly byte[] PngContent = [.. PngHeader, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];

    public StorageSecurityEnforcerTests()
    {
        _optionsMock = new Mock<IOptions<StorageSecuritySetting>>();
        _loggerMock = new Mock<ILogger<StorageSecurityEnforcer>>();
    }

    private StorageSecurityEnforcer CreateSut(Action<StorageSecuritySetting>? configure = null)
    {
        StorageSecuritySetting setting = new()
        {
            AllowedExtensions = new HashSet<string>(StorageSecuritySettingConstant.Defaults.AllowedExtensions, StringComparer.OrdinalIgnoreCase),
            BlockedExtensions = new HashSet<string>(StorageSecuritySettingConstant.Defaults.BlockedExtensions, StringComparer.OrdinalIgnoreCase),
            MaxFileSizeBytes = StorageSecuritySettingConstant.Defaults.MaxFileSizeBytes,
            ValidateMagicBytes = StorageSecuritySettingConstant.Defaults.ValidateMagicBytes,
        };
        configure?.Invoke(setting);
        _optionsMock.Setup(x => x.Value).Returns(setting);
        return new StorageSecurityEnforcer(_optionsMock.Object, _loggerMock.Object);
    }

    private static UploadRequest CreateRequest(string key, byte[] content, string contentType = "application/octet-stream")
        => new()
        { Key = key, Content = new MemoryStream(content), ContentType = contentType };

    private sealed class NonSeekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
    }

    [Fact(DisplayName = "EnforceAsync with blocked extension should return BlockedExtension")]
    public async Task EnforceAsync_WithBlockedExtension_ShouldReturnBlockedExtension()
    {
        StorageSecurityEnforcer sut = CreateSut();

        Result result = await sut.EnforceAsync(CreateRequest("virus.exe", []));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.BlockedExtension");
    }

    [Fact(DisplayName = "EnforceAsync with allowed extension should pass extension checks")]
    public async Task EnforceAsync_WithAllowedExtension_ShouldPassExtensionChecks()
    {
        StorageSecurityEnforcer sut = CreateSut(cfg => cfg.ValidateMagicBytes = false);

        Result result = await sut.EnforceAsync(CreateRequest("photo.jpg", []));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "EnforceAsync with disallowed extension should return ForbiddenExtension")]
    public async Task EnforceAsync_WithDisallowedExtension_ShouldReturnForbiddenExtension()
    {
        StorageSecurityEnforcer sut = CreateSut();

        Result result = await sut.EnforceAsync(CreateRequest("archive.rar", []));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.ForbiddenExtension");
    }

    [Fact(DisplayName = "EnforceAsync with file size under limit should pass size check")]
    public async Task EnforceAsync_WithFileSizeUnderLimit_ShouldPassSizeCheck()
    {
        StorageSecurityEnforcer sut = CreateSut(cfg => cfg.ValidateMagicBytes = false);
        byte[] smallContent = new byte[1024];

        Result result = await sut.EnforceAsync(CreateRequest("document.pdf", smallContent));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "EnforceAsync with file size exactly at limit should pass size check")]
    public async Task EnforceAsync_WithFileSizeAtLimit_ShouldPassSizeCheck()
    {
        StorageSecurityEnforcer sut = CreateSut(cfg =>
        {
            cfg.MaxFileSizeBytes = 1024;
            cfg.ValidateMagicBytes = false;
        });

        Result result = await sut.EnforceAsync(CreateRequest("document.pdf", new byte[1024]));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "EnforceAsync with file size exceeding limit should return FileTooLarge")]
    public async Task EnforceAsync_WithFileSizeExceedingLimit_ShouldReturnFileTooLarge()
    {
        StorageSecurityEnforcer sut = CreateSut(cfg =>
        {
            cfg.MaxFileSizeBytes = 100;
            cfg.ValidateMagicBytes = false;
        });

        Result result = await sut.EnforceAsync(CreateRequest("document.pdf", new byte[101]));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.FileTooLarge");
    }

    [Fact(DisplayName = "EnforceAsync with non-seekable stream should return FileSizeUnknown")]
    public async Task EnforceAsync_WithNonSeekableStream_ShouldReturnFileSizeUnknown()
    {
        StorageSecurityEnforcer sut = CreateSut(cfg => cfg.ValidateMagicBytes = false);
        NonSeekableStream stream = new([.. "hello"u8]);
        UploadRequest request = new() { Key = "file.txt", Content = stream, ContentType = "text/plain" };

        Result result = await sut.EnforceAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.FileSizeUnknown");
    }

    [Fact(DisplayName = "EnforceAsync with valid magic bytes should pass")]
    public async Task EnforceAsync_WithValidMagicBytes_ShouldPass()
    {
        StorageSecurityEnforcer sut = CreateSut();

        Result result = await sut.EnforceAsync(CreateRequest("image.png", PngContent));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "EnforceAsync with invalid magic bytes should return MagicBytesMismatch")]
    public async Task EnforceAsync_WithInvalidMagicBytes_ShouldReturnMagicBytesMismatch()
    {
        StorageSecurityEnforcer sut = CreateSut();

        Result result = await sut.EnforceAsync(CreateRequest("image.png", InvalidHeader));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.MagicBytesMismatch");
    }

    [Fact(DisplayName = "EnforceAsync after magic bytes check should reset stream position to 0 (pass)")]
    public async Task EnforceAsync_AfterMagicBytesCheck_StreamPositionResetToZero_Pass()
    {
        StorageSecurityEnforcer sut = CreateSut();
        MemoryStream stream = new(PngContent);
        UploadRequest request = new() { Key = "image.png", Content = stream, ContentType = "image/png" };

        await sut.EnforceAsync(request);

        stream.Position.Should().Be(0);
    }

    [Fact(DisplayName = "EnforceAsync after magic bytes check failure should reset stream position to 0 (fail)")]
    public async Task EnforceAsync_AfterMagicBytesCheckFailure_StreamPositionResetToZero_Fail()
    {
        StorageSecurityEnforcer sut = CreateSut();
        MemoryStream stream = new(InvalidHeader);
        UploadRequest request = new() { Key = "image.png", Content = stream, ContentType = "image/png" };

        await sut.EnforceAsync(request);

        stream.Position.Should().Be(0);
    }

    [Fact(DisplayName = "EnforceAsync with magic bytes disabled should skip magic bytes check")]
    public async Task EnforceAsync_WithMagicBytesDisabled_ShouldSkipCheck()
    {
        StorageSecurityEnforcer sut = CreateSut(cfg =>
        {
            cfg.ValidateMagicBytes = false;
            cfg.MaxFileSizeBytes = 1024;
        });

        Result result = await sut.EnforceAsync(CreateRequest("image.png", new byte[500]));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "EnforceAsync with empty allowed set should reject all extensions")]
    public async Task EnforceAsync_WithEmptyAllowedSet_ShouldRejectAll()
    {
        StorageSecurityEnforcer sut = CreateSut(cfg =>
        {
            cfg.AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            cfg.ValidateMagicBytes = false;
        });

        Result result = await sut.EnforceAsync(CreateRequest("file.txt", []));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.ForbiddenExtension");
    }

    [Fact(DisplayName = "EnforceAsync with unknown extension should return ForbiddenExtension")]
    public async Task EnforceAsync_WithUnknownExtension_ShouldReturnForbiddenExtension()
    {
        StorageSecurityEnforcer sut = CreateSut(cfg => cfg.ValidateMagicBytes = false);

        Result result = await sut.EnforceAsync(CreateRequest("file.xyz", []));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.ForbiddenExtension");
    }

    [Fact(DisplayName = "EnforceAsync with all checks passing should return Ok")]
    public async Task EnforceAsync_WithAllChecksPassing_ShouldReturnOk()
    {
        StorageSecurityEnforcer sut = CreateSut();

        Result result = await sut.EnforceAsync(CreateRequest("image.png", PngContent));

        result.IsSuccess.Should().BeTrue();
    }
}
