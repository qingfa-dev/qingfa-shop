using Application.BuildingBlocks.Security.Authentication.Tokens;
using Application.BuildingBlocks.Security.Authentication.Tokens.Abstractions.Blacklist;
using Application.BuildingBlocks.Security.Authentication.Tokens.Abstractions.Jwt;
using Application.BuildingBlocks.Security.Authentication.Tokens.Abstractions.Refresh;
using Application.BuildingBlocks.Security.Authentication.Tokens.Abstractions.Rotation;
using Application.BuildingBlocks.Security.Authentication.Tokens.Options;

using Infrastructure.BuildingBlocks.Adapters.Security;
using Infrastructure.BuildingBlocks.Adapters.Security.Authentication.Tokens.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authentication")]
public sealed class SecurityCompositionTest
{
    private const string SigningKey = "qingfa-scaffold-only-signing-key-change-me-32+";

    [Fact(DisplayName = "AddSecurity should register token options and the JWT service")]
    public void AddSecurity_ShouldRegisterTokenServices()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = SigningKey,
            ["Jwt:Issuer"] = "QingFa.Shop",
            ["Jwt:Audience"] = "QingFa.Shop.Clients",
            ["RefreshToken:LifetimeDays"] = "14",
            ["TokenBlacklist:KeyPrefix"] = "blacklist:"
        });
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;

        builder.AddSecurity(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITokenService>().Should().BeOfType<JwtTokenService>();
        provider.GetRequiredService<JwtTokenOption>().Key.Should().Be(SigningKey);
        provider.GetRequiredService<RefreshTokenOption>().LifetimeDays.Should().Be(14);
        provider.GetRequiredService<TokenBlacklistOption>().KeyPrefix.Should().Be("blacklist:");
        provider.GetRequiredService<TimeProvider>().Should().NotBeNull();

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IRefreshTokenService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ITokenBlacklist) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ITokenRotationService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact(DisplayName = "AddSecurity should use defaults when token sections are absent")]
    public void AddSecurity_ShouldUseDefaults()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;

        builder.AddSecurity(new ConfigurationBuilder().Build());

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<JwtTokenOption>().AccessTokenMinutes
            .Should().Be(TokenConstant.Defaults.AccessTokenMinutes);
        provider.GetRequiredService<RefreshTokenOption>().LifetimeDays
            .Should().Be(TokenConstant.Defaults.RefreshTokenDays);
        provider.GetRequiredService<TokenBlacklistOption>().Enabled.Should().BeTrue();
    }

    [Fact(DisplayName = "AddSecurity should surface invalid JWT options when the service is resolved")]
    public void AddSecurity_ShouldSurfaceInvalidOptions()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "too-short",
            ["Jwt:Issuer"] = "QingFa.Shop",
            ["Jwt:Audience"] = "QingFa.Shop.Clients"
        });
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        builder.AddSecurity(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Throws<Microsoft.Extensions.Options.OptionsValidationException>(
            () => provider.GetRequiredService<ITokenService>());
    }

    [Fact(DisplayName = "AddSecurity should reject null dependencies")]
    public void AddSecurity_ShouldRejectNullDependencies()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        WebApplicationBuilder? builder = null;

        Assert.Throws<ArgumentNullException>(() => builder!.AddSecurity(configuration));
        Assert.Throws<ArgumentNullException>(
            () => WebApplication.CreateBuilder().AddSecurity(null!));
    }

    private static IConfiguration BuildConfiguration(
        Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
