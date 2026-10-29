using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Options;
using Application.Common.Security.Authentication.Tokens.Services;

using Infrastructure.Security;
using Infrastructure.Security.Authentication.Tokens;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Unit.Tests.Security;

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
        var services = new ServiceCollection();

        services.AddSecurity(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITokenService>().Should().BeOfType<JwtTokenService>();
        provider.GetRequiredService<JwtTokenOption>().Key.Should().Be(SigningKey);
        provider.GetRequiredService<RefreshTokenOption>().LifetimeDays.Should().Be(14);
        provider.GetRequiredService<TokenBlacklistOption>().KeyPrefix.Should().Be("blacklist:");
        provider.GetRequiredService<TimeProvider>().Should().NotBeNull();
    }

    [Fact(DisplayName = "AddSecurity should use defaults when token sections are absent")]
    public void AddSecurity_ShouldUseDefaults()
    {
        var services = new ServiceCollection();

        services.AddSecurity(new ConfigurationBuilder().Build());

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
        var services = new ServiceCollection();
        services.AddSecurity(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<ITokenService>());
    }

    [Fact(DisplayName = "AddSecurity should reject null dependencies")]
    public void AddSecurity_ShouldRejectNullDependencies()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(() => services!.AddSecurity(configuration));
        Assert.Throws<ArgumentNullException>(
            () => new ServiceCollection().AddSecurity(null!));
    }

    private static IConfiguration BuildConfiguration(
        Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
