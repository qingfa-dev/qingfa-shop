namespace Application.BuildingBlocks.Ports.Storage.Providers.Options;

public static class LocalStorageProviderOutcome
{
    public static class Failure
    {
        public static Error LocalPathRequired => Error.Validation(
            code: Codes.LocalPathRequired,
            message: "Storage.Providers.Local.LocalPath is required");

        public static Error LocalPathInvalid => Error.Validation(
            code: Codes.LocalPathInvalid,
            message: "Storage.Providers.Local.LocalPath must be a valid path");

        public static Error BufferSizeInvalid => Error.Validation(
            code: Codes.BufferSizeInvalid,
            message: "Storage.Providers.Local.BufferSize must be greater than 0");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string LocalPathRequired = "Storage.Providers.Local.LocalPath.Required";
        public const string LocalPathInvalid = "Storage.Providers.Local.LocalPath.Invalid";
        public const string BufferSizeInvalid = "Storage.Providers.Local.BufferSize.Invalid";
    }
}
