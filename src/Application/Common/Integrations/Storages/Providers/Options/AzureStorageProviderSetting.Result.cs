namespace Application.Common.Integrations.Storages.Providers.Options;

public static class AzureStorageProviderResult
{
    public static class Failure
    {
        public static Error ConnectionStringRequired => Error.Validation(
            code: "Storage.Providers.Azure.ConnectionString.Required",
            message: "Storage.Providers.Azure.ConnectionString is required");

        public static Error ContainerNameRequired => Error.Validation(
            code: "Storage.Providers.Azure.ContainerName.Required",
            message: "Storage.Providers.Azure.ContainerName is required");

        public static Error ContainerNameInvalid => Error.Validation(
            code: "Storage.Providers.Azure.ContainerName.Invalid",
            message: "Storage.Providers.Azure.ContainerName must be a valid Azure container name (lowercase alphanumeric and hyphens, 3-63 chars)");

        public static Error BufferSizeInvalid => Error.Validation(
            code: "Storage.Providers.Azure.BufferSize.Invalid",
            message: "Storage.Providers.Azure.BufferSize must be greater than 0");
    }
}
