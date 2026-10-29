namespace Application.Common.Integrations.Storages.Providers.Options;

public static class LocalStorageProviderResult
{
    public static class Failure
    {
        public static Error LocalPathRequired => Error.Validation(
            code: "Storage.Providers.Local.LocalPath.Required",
            message: "Storage.Providers.Local.LocalPath is required");

        public static Error LocalPathInvalid => Error.Validation(
            code: "Storage.Providers.Local.LocalPath.Invalid",
            message: "Storage.Providers.Local.LocalPath must be a valid path");

        public static Error BufferSizeInvalid => Error.Validation(
            code: "Storage.Providers.Local.BufferSize.Invalid",
            message: "Storage.Providers.Local.BufferSize must be greater than 0");
    }
}
