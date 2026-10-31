namespace Application.BuildingBlocks.Ports.Storage.Providers.Options;

public static class AzureStorageProviderConstant
{
    public static class Defaults
    {
        public const string ContainerName = "uploads";

        public const int BufferSize = 65536;
    }

    public static class Constraints
    {
        public const int BufferSizeMin = 1;

        public const int BufferSizeMax = 819200;

        public const int ContainerNameMaxLength = 63;
    }
}
