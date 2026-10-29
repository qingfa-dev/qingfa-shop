namespace Application.Common.Integrations.Storages.Providers.Options;

public static class S3StorageProviderResult
{
    public static class Failure
    {
        public static Error ServiceUrlInvalid => Error.Validation(
            code: "Storage.Providers.S3.ServiceUrl.Invalid",
            message: "Storage.Providers.S3.ServiceUrl is not a valid URL");

        public static Error AccessKeyRequired => Error.Validation(
            code: "Storage.Providers.S3.AccessKey.Required",
            message: "Storage.Providers.S3.AccessKey is required");

        public static Error SecretKeyRequired => Error.Validation(
            code: "Storage.Providers.S3.SecretKey.Required",
            message: "Storage.Providers.S3.SecretKey is required");

        public static Error BucketNameRequired => Error.Validation(
            code: "Storage.Providers.S3.BucketName.Required",
            message: "Storage.Providers.S3.BucketName is required");

        public static Error BucketNameInvalid => Error.Validation(
            code: "Storage.Providers.S3.BucketName.Invalid",
            message: "Storage.Providers.S3.BucketName must be 3\u201363 lowercase alphanumeric or hyphens");

        public static Error RegionRequired => Error.Validation(
            code: "Storage.Providers.S3.Region.Required",
            message: "Storage.Providers.S3.Region is required");

        public static Error RegionInvalid => Error.Validation(
            code: "Storage.Providers.S3.Region.Invalid",
            message: "Storage.Providers.S3.Region must not exceed 50 characters");

        public static Error BufferSizeInvalid => Error.Validation(
            code: "Storage.Providers.S3.BufferSize.Invalid",
            message: "Storage.Providers.S3.BufferSize must be greater than 0");
    }
}
