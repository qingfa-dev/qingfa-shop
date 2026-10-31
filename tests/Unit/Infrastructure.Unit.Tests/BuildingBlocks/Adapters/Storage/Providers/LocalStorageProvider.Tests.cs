using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Application.BuildingBlocks.Ports.Storage.Providers.Options;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Storage.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Storage")]
public sealed class LocalStorageProviderTests : IDisposable
{
    #region Setup / Teardown

    private readonly string _tempRoot;
    private readonly LocalStorageProvider _sut;
    private readonly Mock<ILogger<LocalStorageProvider>> _loggerMock = new();

    public LocalStorageProviderTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var settings = new LocalStorageProviderSetting { LocalPath = _tempRoot };
        var optionsMock = new Mock<IOptions<LocalStorageProviderSetting>>();
        optionsMock.Setup(x => x.Value).Returns(settings);
        _sut = new LocalStorageProvider(optionsMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    private static UploadRequest CreateRequest(string key = "test/file.txt", string content = "hello")
        => new()
        { Key = key, Content = new MemoryStream(Encoding.UTF8.GetBytes(content)), ContentType = "text/plain" };

    private static LocalStorageProvider CreateSutWithNonExistentRoot()
    {
        var settings = new LocalStorageProviderSetting
        {
            LocalPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "does_not_exist")
        };
        var optionsMock = new Mock<IOptions<LocalStorageProviderSetting>>();
        optionsMock.Setup(x => x.Value).Returns(settings);
        return new LocalStorageProvider(optionsMock.Object, Mock.Of<ILogger<LocalStorageProvider>>());
    }

    #endregion

    #region UploadAsync

    [Fact(DisplayName = "UploadAsync should create file and return UploadResult")]
    public async Task UploadAsync_ShouldCreateFileAndReturnResult()
    {
        var request = CreateRequest();
        Result<UploadResult> result = await _sut.UploadAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("test/file.txt");
        result.Value.Provider.Should().Be("local");
        result.Value.SizeBytes.Should().Be(5);
        File.Exists(Path.Combine(_tempRoot, "test", "file.txt")).Should().BeTrue();
    }

    [Fact(DisplayName = "UploadAsync when file system throws should return ProviderError")]
    public async Task UploadAsync_WhenFileSystemThrows_ShouldReturnProviderError()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("data"));
        var invalidKey = Path.GetInvalidFileNameChars()[0].ToString();
        var request = new UploadRequest { Key = invalidKey, Content = ms, ContentType = "text/plain" };

        Result<UploadResult> result = await _sut.UploadAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.ProviderError");
    }

    [Fact(DisplayName = "UploadAsync with deeply nested key should create intermediate directories")]
    public async Task UploadAsync_DeeplyNestedKey_ShouldCreateDirectories()
    {
        var request = CreateRequest("a/b/c/d/e/file.txt", "nested");

        Result<UploadResult> result = await _sut.UploadAsync(request);

        result.IsSuccess.Should().BeTrue();
        File.Exists(Path.Combine(_tempRoot, "a", "b", "c", "d", "e", "file.txt")).Should().BeTrue();
    }

    [Fact(DisplayName = "UploadAsync copies from current stream position")]
    public async Task UploadAsync_CopiesFromCurrentStreamPosition()
    {
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("stream test")) { Position = 0 };
        var request = new UploadRequest { Key = "stream_test.txt", Content = content, ContentType = "text/plain" };

        // Read the first byte — upload should start from position 1
        content.ReadByte();

        Result<UploadResult> result = await _sut.UploadAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.SizeBytes.Should().Be(10);
    }

    #endregion

    #region DownloadAsync

    [Fact(DisplayName = "DownloadAsync should read back uploaded file")]
    public async Task DownloadAsync_ShouldReadBackUploadedFile()
    {
        var request = CreateRequest();
        await _sut.UploadAsync(request);

        Result<DownloadResult> result = await _sut.DownloadAsync("test/file.txt");

        result.IsSuccess.Should().BeTrue();
        using var reader = new StreamReader(result.Value.Content);
        var text = await reader.ReadToEndAsync();
        text.Should().Be("hello");
        result.Value.Info.Key.Should().Be("test/file.txt");
        result.Value.Info.Provider.Should().Be("local");
        result.Value.Info.SizeBytes.Should().Be(5);
    }

    [Fact(DisplayName = "DownloadAsync for non-existent file should return NotFound")]
    public async Task DownloadAsync_NonExistent_ShouldReturnNotFound()
    {
        Result<DownloadResult> result = await _sut.DownloadAsync("nonexistent.txt");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.NotFound");
    }

    #endregion

    #region DeleteAsync

    [Fact(DisplayName = "DeleteAsync should remove file and return success")]
    public async Task DeleteAsync_ShouldRemoveFile()
    {
        await _sut.UploadAsync(CreateRequest());
        Result result = await _sut.DeleteAsync("test/file.txt");

        result.IsSuccess.Should().BeTrue();
        File.Exists(Path.Combine(_tempRoot, "test", "file.txt")).Should().BeFalse();
    }

    [Fact(DisplayName = "DeleteAsync for non-existent file should return NotFound")]
    public async Task DeleteAsync_NonExistent_ShouldReturnNotFound()
    {
        Result result = await _sut.DeleteAsync("nonexistent.txt");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.NotFound");
    }

    #endregion

    #region StatAsync

    [Fact(DisplayName = "StatAsync should return metadata for existing file")]
    public async Task StatAsync_ShouldReturnMetadata()
    {
        await _sut.UploadAsync(CreateRequest());
        Result<StoredObjectInfo> result = await _sut.StatAsync("test/file.txt");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("test/file.txt");
        result.Value.Provider.Should().Be("local");
        result.Value.SizeBytes.Should().Be(5);
        result.Value.LastModifiedUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact(DisplayName = "StatAsync for non-existent file should return NotFound")]
    public async Task StatAsync_NonExistent_ShouldReturnNotFound()
    {
        Result<StoredObjectInfo> result = await _sut.StatAsync("nonexistent.txt");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.NotFound");
    }

    #endregion

    #region ListAsync

    [Fact(DisplayName = "ListAsync should return all files")]
    public async Task ListAsync_ShouldReturnAllFiles()
    {
        await _sut.UploadAsync(CreateRequest("a.txt", "aaa"));
        await _sut.UploadAsync(CreateRequest("b.txt", "bbb"));

        Result<IReadOnlyList<StoredObjectInfo>> result = await _sut.ListAsync(null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(x => x.Key == "a.txt");
        result.Value.Should().Contain(x => x.Key == "b.txt");
    }

    [Fact(DisplayName = "ListAsync with prefix should filter")]
    public async Task ListAsync_WithPrefix_ShouldFilter()
    {
        await _sut.UploadAsync(CreateRequest("images/photo.png", "img"));
        await _sut.UploadAsync(CreateRequest("docs/readme.txt", "doc"));

        Result<IReadOnlyList<StoredObjectInfo>> result = await _sut.ListAsync("images/");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Key.Should().Be("images/photo.png");
    }

    [Fact(DisplayName = "ListAsync on empty directory should return empty")]
    public async Task ListAsync_EmptyDirectory_ShouldReturnEmpty()
    {
        Result<IReadOnlyList<StoredObjectInfo>> result = await _sut.ListAsync(null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "ListAsync when root directory does not exist should return empty")]
    public async Task ListAsync_RootNotExists_ShouldReturnEmpty()
    {
        var sut = CreateSutWithNonExistentRoot();

        Result<IReadOnlyList<StoredObjectInfo>> result = await sut.ListAsync(null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "ListAsync with prefix matching nothing should return empty")]
    public async Task ListAsync_PrefixNoMatch_ShouldReturnEmpty()
    {
        await _sut.UploadAsync(CreateRequest("a.txt", "aaa"));
        await _sut.UploadAsync(CreateRequest("b.txt", "bbb"));

        Result<IReadOnlyList<StoredObjectInfo>> result = await _sut.ListAsync("nonexistent/");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Path Traversal (parameterized)

    public static TheoryData<string> PathTraversalMethods => new()
    {
        "Upload", "Download", "Delete", "Stat"
    };

    [Theory]
    [MemberData(nameof(PathTraversalMethods))]
    public async Task PathTraversalKey_ShouldReturnPathTraversalDetected(string method)
    {
        Result result = method switch
        {
            "Upload" => ToBase(await _sut.UploadAsync(CreateRequest(key: "../../etc/passwd"))),
            "Download" => ToBase(await _sut.DownloadAsync("../../etc/passwd")),
            "Delete" => await _sut.DeleteAsync("../../etc/passwd"),
            "Stat" => ToBase(await _sut.StatAsync("../../etc/passwd")),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.PathTraversal");
    }

    #endregion

    #region Cancellation

    [Fact(DisplayName = "UploadAsync with cancelled token should throw OperationCanceledException")]
    public async Task UploadAsync_CancelledToken_ShouldThrow()
    {
        var request = CreateRequest();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => _sut.UploadAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(DisplayName = "DownloadAsync with cancelled token should throw OperationCanceledException")]
    public async Task DownloadAsync_CancelledToken_ShouldThrow()
    {
        await _sut.UploadAsync(CreateRequest());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => _sut.DownloadAsync("test/file.txt", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(DisplayName = "DeleteAsync with cancelled token should throw OperationCanceledException")]
    public async Task DeleteAsync_CancelledToken_ShouldThrow()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => _sut.DeleteAsync("any.txt", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(DisplayName = "StatAsync with cancelled token should throw OperationCanceledException")]
    public async Task StatAsync_CancelledToken_ShouldThrow()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => _sut.StatAsync("any.txt", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(DisplayName = "ListAsync with cancelled token should throw OperationCanceledException")]
    public async Task ListAsync_CancelledToken_ShouldThrow()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => _sut.ListAsync(null, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region ContentType

    [Fact(DisplayName = "DownloadAsync should return ContentType as null")]
    public async Task DownloadAsync_ShouldReturnNullContentType()
    {
        await _sut.UploadAsync(CreateRequest());

        Result<DownloadResult> result = await _sut.DownloadAsync("test/file.txt");

        result.IsSuccess.Should().BeTrue();
        result.Value.Info.ContentType.Should().BeNull();
    }

    [Fact(DisplayName = "StatAsync should return ContentType as null")]
    public async Task StatAsync_ShouldReturnNullContentType()
    {
        await _sut.UploadAsync(CreateRequest());

        Result<StoredObjectInfo> result = await _sut.StatAsync("test/file.txt");

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().BeNull();
    }

    [Fact(DisplayName = "ListAsync should return ContentType as null for all entries")]
    public async Task ListAsync_ShouldReturnNullContentType()
    {
        await _sut.UploadAsync(CreateRequest("a.txt", "aaa"));

        Result<IReadOnlyList<StoredObjectInfo>> result = await _sut.ListAsync(null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(x => x.ContentType.Should().BeNull());
    }

    #endregion

    private static Result ToBase<T>(Result<T> result) =>
        result.IsSuccess ? Result.Ok() : Result.Fail(result.Errors);
}
