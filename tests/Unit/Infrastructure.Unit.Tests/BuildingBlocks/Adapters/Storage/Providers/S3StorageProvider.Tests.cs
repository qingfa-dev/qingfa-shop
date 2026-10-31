using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Storage.Providers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Storage")]
public sealed class S3StorageProviderTests
{
    private readonly S3StorageProvider _sut;
    private readonly Mock<IOptions<S3StorageProviderSetting>> _optionsMock = new();

    public S3StorageProviderTests()
    {
        var settings = new S3StorageProviderSetting
        {
            AccessKey = "AKIAIOSFODNN7EXAMPLE",
            SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
            BucketName = "my-bucket",
            Region = "eu-west-1"
        };
        _optionsMock.Setup(x => x.Value).Returns(settings);
        _sut = new S3StorageProvider(_optionsMock.Object, Mock.Of<ILogger<S3StorageProvider>>());
    }

    private static UploadRequest CreateRequest(string key = "test/file.txt")
        => new()
        { Key = key, Content = new MemoryStream("data"u8.ToArray()), ContentType = "text/plain" };

    [Fact(DisplayName = "UploadAsync should return UploadResult with correct Provider and Key")]
    public async Task UploadAsync_ShouldReturnCorrectResult()
    {
        Result<UploadResult> result = await _sut.UploadAsync(CreateRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value.Provider.Should().Be("s3");
        result.Value.Key.Should().Be("test/file.txt");
        result.Value.SizeBytes.Should().Be(0);
    }

    [Fact(DisplayName = "UploadAsync should construct URI with region-based pattern")]
    public async Task UploadAsync_ShouldUseRegionBasedUri()
    {
        Result<UploadResult> result = await _sut.UploadAsync(CreateRequest("path/to/object.png"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Uri.Should().NotBeNull();
        result.Value.Uri!.ToString().Should().Be("https://my-bucket.s3.eu-west-1.amazonaws.com/path/to/object.png");
    }

    [Fact(DisplayName = "UploadAsync should use ServiceUrl when configured")]
    public async Task UploadAsync_ShouldUseServiceUrl()
    {
        _optionsMock.Setup(x => x.Value).Returns(new S3StorageProviderSetting
        {
            AccessKey = "AKIAIOSFODNN7EXAMPLE",
            SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
            ServiceUrl = "https://play.min.io",
            BucketName = "my-bucket",
            Region = "us-east-1"
        });
        var sut = new S3StorageProvider(_optionsMock.Object, Mock.Of<ILogger<S3StorageProvider>>());

        Result<UploadResult> result = await sut.UploadAsync(CreateRequest("obj.png"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Uri!.ToString().Should().Be("https://play.min.io/obj.png");
    }

    [Fact(DisplayName = "DownloadAsync should return NotImplemented")]
    public async Task DownloadAsync_ShouldReturnNotImplemented()
    {
        Result<DownloadResult> result = await _sut.DownloadAsync("test.txt");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.NotImplemented");
    }

    [Fact(DisplayName = "DeleteAsync should return success")]
    public async Task DeleteAsync_ShouldReturnSuccess()
    {
        Result result = await _sut.DeleteAsync("test.txt");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "StatAsync should return NotImplemented")]
    public async Task StatAsync_ShouldReturnNotImplemented()
    {
        Result<StoredObjectInfo> result = await _sut.StatAsync("test.txt");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Storage.NotImplemented");
    }

    [Fact(DisplayName = "ListAsync should return empty list")]
    public async Task ListAsync_ShouldReturnEmptyList()
    {
        Result<IReadOnlyList<StoredObjectInfo>> result = await _sut.ListAsync(null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
