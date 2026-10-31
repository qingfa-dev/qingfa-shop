namespace Application.BuildingBlocks.Ports.Storage.Providers.Options;

public static class S3StorageProviderOutcome
{
    public static class Failure
    {
        public static Error ServiceUrlInvalid => Error.Validation(
            code: Codes.ServiceUrlInvalid,
            message: "Storage.Providers.S3.ServiceUrl is not a valid URL");

        public static Error AccessKeyRequired => Error.Validation(
            code: Codes.AccessKeyRequired,
            message: "Storage.Providers.S3.AccessKey is required");

        public static Error SecretKeyRequired => Error.Validation(
            code: Codes.SecretKeyRequired,
            message: "Storage.Providers.S3.SecretKey is required");

        public static Error BucketNameRequired => Error.Validation(
            code: Codes.BucketNameRequired,
            message: "Storage.Providers.S3.BucketName is required");

        public static Error BucketNameInvalid => Error.Validation(
            code: Codes.BucketNameInvalid,
            message: "Storage.Providers.S3.BucketName must be 3\u201363 lowercase alphanumeric or hyphens");

        public static Error RegionRequired => Error.Validation(
            code: Codes.RegionRequired,
            message: "Storage.Providers.S3.Region is required");

        public static Error RegionInvalid => Error.Validation(
            code: Codes.RegionInvalid,
            message: "Storage.Providers.S3.Region must not exceed 50 characters");

        public static Error BufferSizeInvalid => Error.Validation(
            code: Codes.BufferSizeInvalid,
            message: "Storage.Providers.S3.BufferSize must be greater than 0");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ServiceUrlInvalid = "Storage.Providers.S3.ServiceUrl.Invalid";
        public const string AccessKeyRequired = "Storage.Providers.S3.AccessKey.Required";
        public const string SecretKeyRequired = "Storage.Providers.S3.SecretKey.Required";
        public const string BucketNameRequired = "Storage.Providers.S3.BucketName.Required";
        public const string BucketNameInvalid = "Storage.Providers.S3.BucketName.Invalid";
        public const string RegionRequired = "Storage.Providers.S3.Region.Required";
        public const string RegionInvalid = "Storage.Providers.S3.Region.Invalid";
        public const string BufferSizeInvalid = "Storage.Providers.S3.BufferSize.Invalid";
    }
}
