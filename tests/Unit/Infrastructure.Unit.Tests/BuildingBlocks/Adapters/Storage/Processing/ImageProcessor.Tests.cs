using Microsoft.Extensions.Logging;

using SkiaSharp;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Storage.Processing;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Storage")]
public sealed class ImageProcessorTests
{
    private static readonly ILogger<ImageProcessor> Logger = Mock.Of<ILogger<ImageProcessor>>();

    private static ImageProcessor CreateSut() => new(Logger);

    [Fact(DisplayName = "ProcessAsync with both dimensions and output format null should return original stream")]
    public async Task ProcessAsync_WithNullDimensionsAndFormat_ShouldReturnOriginalStream()
    {
        using MemoryStream input = CreateTestImageStream(100, 80);
        ImageProcessor sut = CreateSut();
        var opts = new UploadOptions();

        Result<Stream> result = await sut.ProcessAsync(input, opts);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(input);
    }

    [Fact(DisplayName = "ProcessAsync with negative width should return InvalidDimensions")]
    public async Task ProcessAsync_WithInvalidNegativeWidth_ShouldReturnInvalidDimensions()
    {
        using MemoryStream input = CreateTestImageStream(100, 80);
        ImageProcessor sut = CreateSut();
        var opts = new UploadOptions { ResizeWidth = -1, ResizeHeight = 100 };

        Result<Stream> result = await sut.ProcessAsync(input, opts);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Processing.InvalidDimensions");
    }

    [Fact(DisplayName = "ProcessAsync with zero height should return InvalidDimensions")]
    public async Task ProcessAsync_WithInvalidZeroHeight_ShouldReturnInvalidDimensions()
    {
        using MemoryStream input = CreateTestImageStream(100, 80);
        ImageProcessor sut = CreateSut();
        var opts = new UploadOptions { ResizeWidth = 100, ResizeHeight = 0 };

        Result<Stream> result = await sut.ProcessAsync(input, opts);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Processing.InvalidDimensions");
    }

    [Fact(DisplayName = "ProcessAsync with invalid image stream should return InvalidImage")]
    public async Task ProcessAsync_WithInvalidImageStream_ShouldReturnInvalidImage()
    {
        byte[] garbage = "this is not an image"u8.ToArray();
        using var input = new MemoryStream(garbage);
        ImageProcessor sut = CreateSut();
        var opts = new UploadOptions { ResizeWidth = 100, ResizeHeight = 100 };

        Result<Stream> result = await sut.ProcessAsync(input, opts);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Processing.InvalidImage");
    }

    [Fact(DisplayName = "ProcessAsync with Fit mode should maintain aspect ratio within bounds")]
    public async Task ProcessAsync_WithFitMode_ShouldMaintainAspectRatioWithinBounds()
    {
        using MemoryStream input = CreateTestImageStream(200, 100);
        ImageProcessor sut = CreateSut();
        var opts = new UploadOptions { ResizeWidth = 100, ResizeHeight = 100, ResizeMode = ResizeMode.Fit };

        Result<Stream> result = await sut.ProcessAsync(input, opts);

        result.IsSuccess.Should().BeTrue();
        using var output = SKBitmap.Decode(result.Value);
        output.Width.Should().Be(100);
        output.Height.Should().Be(50);
    }

    [Fact(DisplayName = "ProcessAsync with Fill mode should crop overflow")]
    public async Task ProcessAsync_WithFillMode_ShouldCropOverflow()
    {
        using MemoryStream input = CreateTestImageStream(200, 100);
        ImageProcessor sut = CreateSut();
        var opts = new UploadOptions { ResizeWidth = 100, ResizeHeight = 100, ResizeMode = ResizeMode.Fill };

        Result<Stream> result = await sut.ProcessAsync(input, opts);

        result.IsSuccess.Should().BeTrue();
        using var output = SKBitmap.Decode(result.Value);
        output.Width.Should().Be(100);
        output.Height.Should().Be(100);
    }

    [Fact(DisplayName = "ProcessAsync with Stretch mode should ignore aspect ratio")]
    public async Task ProcessAsync_WithStretchMode_ShouldIgnoreAspectRatio()
    {
        using MemoryStream input = CreateTestImageStream(200, 100);
        ImageProcessor sut = CreateSut();
        var opts = new UploadOptions { ResizeWidth = 50, ResizeHeight = 80, ResizeMode = ResizeMode.Stretch };

        Result<Stream> result = await sut.ProcessAsync(input, opts);

        result.IsSuccess.Should().BeTrue();
        using var output = SKBitmap.Decode(result.Value);
        output.Width.Should().Be(50);
        output.Height.Should().Be(80);
    }

    [Fact(DisplayName = "ProcessAsync with MaintainAspectRatio false should stretch regardless of mode")]
    public async Task ProcessAsync_WithMaintainAspectRatioFalse_ShouldStretchRegardlessOfMode()
    {
        using MemoryStream input = CreateTestImageStream(200, 100);
        ImageProcessor sut = CreateSut();
        var opts = new UploadOptions { ResizeWidth = 50, ResizeHeight = 80, MaintainAspectRatio = false };

        Result<Stream> result = await sut.ProcessAsync(input, opts);

        result.IsSuccess.Should().BeTrue();
        using var output = SKBitmap.Decode(result.Value);
        output.Width.Should().Be(50);
        output.Height.Should().Be(80);
    }

    [Fact(DisplayName = "ProcessAsync with output format should convert format")]
    public async Task ProcessAsync_WithOutputFormat_ShouldConvertFormat()
    {
        using MemoryStream input = CreateTestImageStream(100, 80);
        ImageProcessor sut = CreateSut();
        var opts = new UploadOptions { ResizeWidth = 50, ResizeHeight = 50, OutputFormat = "jpeg" };

        Result<Stream> result = await sut.ProcessAsync(input, opts);

        result.IsSuccess.Should().BeTrue();
        using var ms = new MemoryStream();
        await result.Value.CopyToAsync(ms);
        byte[] data = ms.ToArray();
        data[0].Should().Be(0xFF);
        data[1].Should().Be(0xD8);
        data[2].Should().Be(0xFF);
    }

    [Fact(DisplayName = "ProcessAsync with unsupported format should return UnsupportedFormat")]
    public async Task ProcessAsync_WithUnsupportedFormat_ShouldReturnUnsupportedFormat()
    {
        using MemoryStream input = CreateTestImageStream(100, 80);
        ImageProcessor sut = CreateSut();
        var opts = new UploadOptions { ResizeWidth = 50, ResizeHeight = 50, OutputFormat = "tiff" };

        Result<Stream> result = await sut.ProcessAsync(input, opts);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Processing.UnsupportedFormat");
    }

    [Fact(DisplayName = "ProcessAsync with null output format should preserve input format")]
    public async Task ProcessAsync_WithNullOutputFormat_ShouldPreserveInputFormat()
    {
        using MemoryStream input = CreateTestImageStream(100, 80);
        ImageProcessor sut = CreateSut();
        var opts = new UploadOptions { ResizeWidth = 50, ResizeHeight = 50 };

        Result<Stream> result = await sut.ProcessAsync(input, opts);

        result.IsSuccess.Should().BeTrue();
        using var ms = new MemoryStream();
        await result.Value.CopyToAsync(ms);
        byte[] data = ms.ToArray();
        data[0].Should().Be(0x89);
        data[1].Should().Be(0x50);
        data[2].Should().Be(0x4E);
        data[3].Should().Be(0x47);
    }

    private static MemoryStream CreateTestImageStream(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Position = 0;
        return stream;
    }
}
