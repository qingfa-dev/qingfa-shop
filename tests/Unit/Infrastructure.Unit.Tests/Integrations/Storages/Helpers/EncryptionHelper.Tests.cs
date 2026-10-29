using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Unit.Tests.Integrations.Storages.Helpers;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Storage")]
public sealed class EncryptionHelperTests
{
    private static readonly byte[] TestKey = Encoding.UTF8.GetBytes("ThisIsA32ByteKeyForAES256!!!");

    [Fact(DisplayName = "Encrypt then Decrypt should return original content")]
    public async Task EncryptThenDecrypt_ShouldReturnOriginal()
    {
        byte[] original = "Hello, World! This is a test."u8.ToArray();
        using var plaintext = new MemoryStream(original);

        Stream encrypted = await EncryptionHelper.EncryptAsync(plaintext, TestKey);
        Stream decrypted = await EncryptionHelper.DecryptAsync(encrypted, TestKey);

        using var reader = new StreamReader(decrypted);
        string result = await reader.ReadToEndAsync();
        result.Should().Be("Hello, World! This is a test.");
    }

    [Fact(DisplayName = "Decrypt with wrong key should throw")]
    public async Task Decrypt_WithWrongKey_ShouldThrow()
    {
        byte[] original = "sensitive data"u8.ToArray();
        using var plaintext = new MemoryStream(original);
        byte[] wrongKey = Encoding.UTF8.GetBytes("ThisIsADifferent32ByteKeyForTest!!");

        Stream encrypted = await EncryptionHelper.EncryptAsync(plaintext, TestKey);

        Func<Task> act = () => EncryptionHelper.DecryptAsync(encrypted, wrongKey);
        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact(DisplayName = "Encrypt with same key and data should produce different ciphertext each time (different IV)")]
    public async Task Encrypt_WithSameKey_ShouldProduceDifferentCiphertext()
    {
        byte[] data = "repeatable test"u8.ToArray();

        string cipher1;
        string cipher2;
        using (var stream1 = new MemoryStream(data))
        using (var stream2 = new MemoryStream(data))
        {
            Stream encrypted1 = await EncryptionHelper.EncryptAsync(stream1, TestKey);
            Stream encrypted2 = await EncryptionHelper.EncryptAsync(stream2, TestKey);

            using var reader1 = new StreamReader(encrypted1);
            using var reader2 = new StreamReader(encrypted2);
            cipher1 = await reader1.ReadToEndAsync();
            cipher2 = await reader2.ReadToEndAsync();
        }

        cipher1.Should().NotBe(cipher2);
    }

    [Fact(DisplayName = "Encrypt with short key should derive 32 bytes via SHA-256")]
    public async Task Encrypt_WithShortKey_ShouldDeriveKey()
    {
        byte[] shortKey = "short"u8.ToArray();
        byte[] data = "content"u8.ToArray();
        using var plaintext = new MemoryStream(data);

        Stream encrypted = await EncryptionHelper.EncryptAsync(plaintext, shortKey);
        Stream decrypted = await EncryptionHelper.DecryptAsync(encrypted, shortKey);

        using var reader = new StreamReader(decrypted);
        string result = await reader.ReadToEndAsync();
        result.Should().Be("content");
    }

    [Fact(DisplayName = "Encrypt with exactly 32-byte key should use it directly")]
    public async Task Encrypt_WithExact32ByteKey_ShouldUseDirectly()
    {
        byte[] exactKey = Encoding.UTF8.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZ123456");
        exactKey.Length.Should().Be(32);
        byte[] data = "data"u8.ToArray();
        using var plaintext = new MemoryStream(data);

        Stream encrypted = await EncryptionHelper.EncryptAsync(plaintext, exactKey);
        Stream decrypted = await EncryptionHelper.DecryptAsync(encrypted, exactKey);

        using var reader = new StreamReader(decrypted);
        string result = await reader.ReadToEndAsync();
        result.Should().Be("data");
    }
}
