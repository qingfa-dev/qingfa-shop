using Application.Common.Security.Authentication.Contexts;
using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Abstractions.Blacklist;
using Application.Common.Security.Authentication.Tokens.Abstractions.Jwt;
using Application.Common.Security.Authentication.Tokens.Abstractions.Refresh;
using Application.Common.Security.Authentication.Tokens.Abstractions.Rotation;
using Application.Common.Security.Authentication.Tokens.Options;

using Infrastructure.Security.Authentication;
using Infrastructure.Security.Authentication.Contexts;
using Infrastructure.Security.Authentication.Tokens.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Unit.Tests.Security.Authentication;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authentication")]
public sealed class AuthenticationCompositionTest
{
    private const string SigningKey = "qingfa-scaffold-only-signing-key-change-me-32+";

    [Fact(DisplayName = "AddAuthentication should register contexts, options, and token services")]
    public void AddAuthentication_ShouldRegisterServices()
    {
        IConfiguration configuration = BuildConfiguration();
        var services = new ServiceCollection();

        services.AddAuthentication(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITenantContext>().Should().BeOfType<TenantContext>();
        provider.GetRequiredService<IUserContext>().Should().BeOfType<CurrentUser>();
        provider.GetRequiredService<ITokenService>().Should().BeOfType<JwtTokenService>();
        provider.GetRequiredService<JwtTokenOption>().Key.Should().Be(SigningKey);

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

    [Fact(DisplayName = "AddAuthentication should reject null dependencies")]
    public void AddAuthentication_ShouldRejectNullDependencies()
    {
        IConfiguration configuration = BuildConfiguration();
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(() => services!.AddAuthentication(configuration));
        Assert.Throws<ArgumentNullException>(
            () => new ServiceCollection().AddAuthentication((IConfiguration)null!));
    }

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = SigningKey,
                ["Jwt:Issuer"] = "QingFa.Shop",
                ["Jwt:Audience"] = "QingFa.Shop.Clients"
            })
            .Build();
}
