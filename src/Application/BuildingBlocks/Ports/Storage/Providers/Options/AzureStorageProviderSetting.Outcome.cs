namespace Application.BuildingBlocks.Ports.Storage.Providers.Options;

public static class AzureStorageProviderOutcome
{
    public static class Failure
    {
        public static Error ConnectionStringRequired => Error.Validation(
            code: Codes.ConnectionStringRequired,
            message: "Storage.Providers.Azure.ConnectionString is required");

        public static Error ContainerNameRequired => Error.Validation(
            code: Codes.ContainerNameRequired,
            message: "Storage.Providers.Azure.ContainerName is required");

        public static Error ContainerNameInvalid => Error.Validation(
            code: Codes.ContainerNameInvalid,
            message: "Storage.Providers.Azure.ContainerName must be a valid Azure container name (lowercase alphanumeric and hyphens, 3-63 chars)");

        public static Error BufferSizeInvalid => Error.Validation(
            code: Codes.BufferSizeInvalid,
            message: "Storage.Providers.Azure.BufferSize must be greater than 0");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ConnectionStringRequired = "Storage.Providers.Azure.ConnectionString.Required";
        public const string ContainerNameRequired = "Storage.Providers.Azure.ContainerName.Required";
        public const string ContainerNameInvalid = "Storage.Providers.Azure.ContainerName.Invalid";
        public const string BufferSizeInvalid = "Storage.Providers.Azure.BufferSize.Invalid";
    }
}
