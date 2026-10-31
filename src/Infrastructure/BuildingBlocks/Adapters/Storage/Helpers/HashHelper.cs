using System.Security.Cryptography;

namespace Infrastructure.BuildingBlocks.Adapters.Storage.Helpers;

public static class HashHelper
{
    public static async Task<string> ComputeHashAsync(Stream stream, CancellationToken ct = default)
    {
        byte[] hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexStringLower(hash);
    }

    public static string ComputeHash(byte[] data)
    {
        byte[] hash = SHA256.HashData(data);
        return Convert.ToHexStringLower(hash);
    }
}
