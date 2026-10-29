using System.Security.Cryptography;

namespace Infrastructure.Integrations.Storages.Helpers;

public static class EncryptionHelper
{
    private const int KeySizeBytes = 32;
    private const int IvSizeBytes = 16;

    public static async Task<Stream> EncryptAsync(Stream plaintext, byte[] key, CancellationToken ct = default)
    {
        byte[] derivedKey = DeriveKey(key);
        using Aes aes = Aes.Create();
        aes.Key = derivedKey;
        aes.GenerateIV();

        var outputStream = new MemoryStream();
        await outputStream.WriteAsync(aes.IV.AsMemory(0, IvSizeBytes), ct);

        await using var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true);
        await plaintext.CopyToAsync(cryptoStream, ct);
        cryptoStream.FlushFinalBlock();

        outputStream.Position = 0;
        return outputStream;
    }

    public static async Task<Stream> DecryptAsync(Stream ciphertext, byte[] key, CancellationToken ct = default)
    {
        byte[] derivedKey = DeriveKey(key);

        byte[] iv = new byte[IvSizeBytes];
        int bytesRead = await ciphertext.ReadAsync(iv.AsMemory(0, IvSizeBytes), ct);
        if (bytesRead < IvSizeBytes)
            throw new InvalidDataException("Ciphertext is too short to contain IV.");

        using Aes aes = Aes.Create();
        aes.Key = derivedKey;
        aes.IV = iv;

        var outputStream = new MemoryStream();
        await using var cryptoStream = new CryptoStream(outputStream, aes.CreateDecryptor(), CryptoStreamMode.Write, leaveOpen: true);
        await ciphertext.CopyToAsync(cryptoStream, ct);
        cryptoStream.FlushFinalBlock();

        outputStream.Position = 0;
        return outputStream;
    }

    private static byte[] DeriveKey(byte[] key)
    {
        if (key.Length == KeySizeBytes)
            return key;
        return SHA256.HashData(key);
    }
}
