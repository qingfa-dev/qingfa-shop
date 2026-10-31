using FluentValidation;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Application.BuildingBlocks.UseCases.Validation.FluentValidation;
using Infrastructure.BuildingBlocks.Adapters.Storage.Services;
using Infrastructure.BuildingBlocks.Adapters.Storage.Security.Scanners;
using Infrastructure.BuildingBlocks.Adapters.Storage.Security;
using Infrastructure.BuildingBlocks.Adapters.Storage.Providers;
using Infrastructure.BuildingBlocks.Adapters.Storage.Processing;

using StorageAntiForgeryGuard = Infrastructure.BuildingBlocks.Adapters.Storage.Security.Guard.StorageAntiForgeryGuard;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.BuildingBlocks.Adapters.Storage;

public static class StorageExtensions
{
    public static WebApplicationBuilder AddStorage(this WebApplicationBuilder builder)
    {
        builder.Services.AddStorageOptions();
        builder.Services.AddStorageServices();
        return builder;
    }

    private static void AddStorageOptions(this IServiceCollection services)
    {
        services.AddAntiforgery();
        services.AddSingleton<IValidator<StorageSetting>, StorageSettingValidator>();
        services.AddOptions<StorageSetting>()
            .BindConfiguration(StorageSetting.SectionName)
            .ValidateFluentValidation()
            .ValidateOnStart();

        services.AddSingleton<IValidator<StorageSecuritySetting>, StorageSecuritySettingValidator>();
        services.AddOptions<StorageSecuritySetting>()
            .BindConfiguration(StorageSecuritySetting.SectionName)
            .ValidateFluentValidation()
            .ValidateOnStart();

        services.AddSingleton<IValidator<LocalStorageProviderSetting>, LocalStorageProviderSettingValidator>();
        services.AddOptions<LocalStorageProviderSetting>()
            .BindConfiguration($"{BaseStorageProviderSetting.BaseSection}:{LocalStorageProviderSetting.ProviderKey}")
            .ValidateFluentValidation()
            .ValidateOnStart();

        services.AddSingleton<IValidator<AzureStorageProviderSetting>, AzureStorageProviderSettingValidator>();
        services.AddOptions<AzureStorageProviderSetting>()
            .BindConfiguration($"{BaseStorageProviderSetting.BaseSection}:{AzureStorageProviderSetting.ProviderKey}")
            .ValidateFluentValidation()
            .ValidateOnStart();

        services.AddSingleton<IValidator<S3StorageProviderSetting>, S3StorageProviderSettingValidator>();
        services.AddOptions<S3StorageProviderSetting>()
            .BindConfiguration($"{BaseStorageProviderSetting.BaseSection}:{S3StorageProviderSetting.ProviderKey}")
            .ValidateFluentValidation()
            .ValidateOnStart();

        services.AddSingleton<IStorageSecurityEnforcer, StorageSecurityEnforcer>();
        services.AddSingleton<IValidator<AntiForgeryOptions>, AntiForgeryOptionsValidator>();
        services.AddOptions<AntiForgeryOptions>()
            .BindConfiguration(AntiForgeryOptions.SectionName)
            .ValidateFluentValidation()
            .ValidateOnStart();

        services.AddSingleton<IStorageAntiForgeryGuard, StorageAntiForgeryGuard>();

        services.AddSingleton<IValidator<StorageMalwareScannerOptions>, StorageMalwareScannerOptionsValidator>();
        services.AddOptions<StorageMalwareScannerOptions>()
            .BindConfiguration(StorageMalwareScannerOptions.SectionName)
            .ValidateFluentValidation()
            .ValidateOnStart();
    }

    private static void AddStorageServices(this IServiceCollection services)
    {
        services.AddSingleton<StorageMalwareScanner>();
        services.AddSingleton<ContentMalwareScanner>();
        services.AddSingleton<LocalStorageProvider>();
        services.AddSingleton<S3StorageProvider>();
        services.AddSingleton<IStorageMalwareScanner>(sp =>
            new AggregateMalwareScanner(
                [sp.GetRequiredService<StorageMalwareScanner>(), sp.GetRequiredService<ContentMalwareScanner>()],
                sp.GetRequiredService<ILogger<AggregateMalwareScanner>>()));

        services.AddSingleton<IImageProcessor, ImageProcessor>();

        services.AddSingleton<IStorageService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<StorageSetting>>();
            var providers = new Dictionary<string, IStorageProvider>(StringComparer.OrdinalIgnoreCase)
            {
                [sp.GetRequiredService<LocalStorageProvider>().Name] = sp.GetRequiredService<LocalStorageProvider>(),
                [sp.GetRequiredService<S3StorageProvider>().Name] = sp.GetRequiredService<S3StorageProvider>(),
            };

            return new StorageService(
                providers,
                options.Value.DefaultProvider,
                sp.GetRequiredService<IStorageSecurityEnforcer>(),
                sp.GetRequiredService<IStorageAntiForgeryGuard>(),
                sp.GetRequiredService<IHttpContextAccessor>(),
                sp.GetRequiredService<ILogger<StorageService>>(),
                sp.GetService<IStorageMalwareScanner>(),
                sp.GetService<IImageProcessor>(),
                sp.GetService<IOptions<StorageSecuritySetting>>());
        });
    }

    public static WebApplication UseStorage(this WebApplication app)
    {
        return app;
    }
}
