namespace Infrastructure.Unit.Tests.Integrations.Storages.Helpers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Storage")]
public sealed class HashHelperTests
{
    [Fact(DisplayName = "ComputeHashAsync should return known SHA-256 hex for 'hello'")]
    public async Task ComputeHashAsync_WithHello_ShouldReturnKnownHash()
    {
        using var stream = new MemoryStream("hello"u8.ToArray());

        string hash = await HashHelper.ComputeHashAsync(stream);

        hash.Should().Be("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
    }

    [Fact(DisplayName = "ComputeHashAsync with empty stream should return valid hash")]
    public async Task ComputeHashAsync_WithEmptyStream_ShouldReturnValidHash()
    {
        using var stream = new MemoryStream([]);

        string hash = await HashHelper.ComputeHashAsync(stream);

        hash.Should().Be("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    [Fact(DisplayName = "ComputeHashAsync should be deterministic")]
    public async Task ComputeHashAsync_ShouldBeDeterministic()
    {
        byte[] data = "deterministic test data"u8.ToArray();

        string hash1;
        string hash2;
        using (var stream1 = new MemoryStream(data))
        using (var stream2 = new MemoryStream(data))
        {
            hash1 = await HashHelper.ComputeHashAsync(stream1);
            hash2 = await HashHelper.ComputeHashAsync(stream2);
        }

        hash1.Should().Be(hash2);
    }

    [Fact(DisplayName = "ComputeHash should match ComputeHashAsync for same data")]
    public async Task ComputeHash_ShouldMatchAsyncVersion()
    {
        byte[] data = "hello"u8.ToArray();
        string expectedHash;
        using (var stream = new MemoryStream(data))
        {
            expectedHash = await HashHelper.ComputeHashAsync(stream);
        }

        string hash = HashHelper.ComputeHash(data);

        hash.Should().Be(expectedHash);
    }
}
