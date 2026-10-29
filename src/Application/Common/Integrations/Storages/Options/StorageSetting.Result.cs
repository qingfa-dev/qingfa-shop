namespace Application.Common.Integrations.Storages.Options;

public static class StorageSettingResult
{
    public static class Failure
    {
        public static Error DefaultProviderRequired => Error.Validation(
            code: "Storage.DefaultProvider.Required",
            message: "Storage.DefaultProvider is required");

        public static Error DefaultProviderInvalid => Error.Validation(
            code: "Storage.DefaultProvider.Invalid",
            message: "Storage.DefaultProvider must be a valid provider name");

        public static Error BaseUrlInvalid => Error.Validation(
            code: "Storage.BaseUrl.Invalid",
            message: "Storage.BaseUrl must be a valid URL or empty");

        public static Error SecurityRequired => Error.Validation(
            code: "Storage.Security.Required",
            message: "Storage.Security is required");

    }
}
