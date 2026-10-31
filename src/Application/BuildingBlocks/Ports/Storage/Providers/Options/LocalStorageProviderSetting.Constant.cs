namespace Application.BuildingBlocks.Ports.Storage.Providers.Options;

public static class LocalStorageProviderConstant
{
    public static class Defaults
    {
        public const string LocalPath = "storage";

        public const int BufferSize = 81920;
    }

    public static class Constraints
    {
        public const int LocalPathMaxLength = 260;

        public const int BufferSizeMin = 1;

        public const int BufferSizeMax = 819200;
    }
}
