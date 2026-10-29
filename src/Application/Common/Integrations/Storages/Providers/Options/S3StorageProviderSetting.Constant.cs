namespace Application.Common.Integrations.Storages.Providers.Options;

public static class S3StorageProviderConstant
{
    public static class Defaults
    {
        public const string BucketName = "uploads";

        public const string Region = "us-east-1";

        public const int BufferSize = 65536;
    }

    public static class Constraints
    {
        public const int AccessKeyMaxLength = 128;

        public const int SecretKeyMaxLength = 256;

        public const int BucketNameMinLength = 3;

        public const int BucketNameMaxLength = 63;

        public const int RegionMaxLength = 50;

        public const int ServiceUrlMaxLength = 2048;

        public const int BufferSizeMin = 1;

        public const int BufferSizeMax = 819200;
    }
}
