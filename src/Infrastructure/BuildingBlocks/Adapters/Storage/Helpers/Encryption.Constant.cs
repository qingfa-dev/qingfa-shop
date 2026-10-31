namespace Infrastructure.BuildingBlocks.Adapters.Storage.Helpers;

/// <summary>Byte sizes used by the symmetric encryption helper.</summary>
public static class EncryptionConstant
{
    /// <summary>AES-256 key size.</summary>
    public const int KeySizeBytes = 32;

    /// <summary>AES block/IV size.</summary>
    public const int IvSizeBytes = 16;
}
