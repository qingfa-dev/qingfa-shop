using Application.BuildingBlocks.Ports.Storage.Options;
using Application.BuildingBlocks.Ports.Storage.Providers.Options;

using Infrastructure.BuildingBlocks.Adapters.Storage;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Storage.Options;

/// <summary>Proves storage options are validated when the host starts, not on first use.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Storage")]
public sealed class StorageOptionsStartupTest
{
    public static TheoryData<string, string, Type> InvalidOptions => new()
    {
        { "Storage:DefaultProvider", string.Empty, typeof(StorageSetting) },
        { "Storage:Providers:Local:BufferSize", "0", typeof(LocalStorageProviderSetting) }
    };

    [Theory(DisplayName = "Invalid storage options should fail through the startup validator")]
    [MemberData(nameof(InvalidOptions))]
    public void InvalidStorageOptions_ShouldFailAtStartup(
        string key,
        string value,
        Type optionsType)
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [key] = value
        });
        builder.AddStorage();

        using WebApplication app = builder.Build();
        IStartupValidator startupValidator = app.Services.GetRequiredService<IStartupValidator>();

        // Act
        Exception? thrown = null;
        try
        {
            startupValidator.Validate();
        }
        catch (Exception exception)
        {
            thrown = exception;
        }

        // Assert
        thrown.Should().NotBeNull();
        IEnumerable<OptionsValidationException> failures = thrown switch
        {
            AggregateException aggregate => aggregate
                .Flatten()
                .InnerExceptions
                .OfType<OptionsValidationException>(),
            OptionsValidationException single => [single],
            _ => []
        };

        failures.Should().Contain(failure => failure.OptionsType == optionsType);
    }
}
