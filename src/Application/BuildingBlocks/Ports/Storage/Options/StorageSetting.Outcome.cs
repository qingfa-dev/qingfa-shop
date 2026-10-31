namespace Application.BuildingBlocks.Ports.Storage.Options;

public static class StorageSettingOutcome
{
    public static class Failure
    {
        public static Error DefaultProviderRequired => Error.Validation(
            code: Codes.DefaultProviderRequired,
            message: "Storage.DefaultProvider is required");

        public static Error DefaultProviderInvalid => Error.Validation(
            code: Codes.DefaultProviderInvalid,
            message: "Storage.DefaultProvider must be a valid provider name");

        public static Error BaseUrlInvalid => Error.Validation(
            code: Codes.BaseUrlInvalid,
            message: "Storage.BaseUrl must be a valid URL or empty");

        public static Error SecurityRequired => Error.Validation(
            code: Codes.SecurityRequired,
            message: "Storage.Security is required");

    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string DefaultProviderRequired = "Storage.DefaultProvider.Required";
        public const string DefaultProviderInvalid = "Storage.DefaultProvider.Invalid";
        public const string BaseUrlInvalid = "Storage.BaseUrl.Invalid";
        public const string SecurityRequired = "Storage.Security.Required";
    }
}
