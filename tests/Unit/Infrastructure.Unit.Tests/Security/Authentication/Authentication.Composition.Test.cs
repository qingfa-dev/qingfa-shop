using Application.Common.Security.Authentication.Contexts;
using Application.Common.Security.Authentication.Tokens.Abstractions.Blacklist;
using Application.Common.Security.Authentication.Tokens.Abstractions.Jwt;
using Application.Common.Security.Authentication.Tokens.Abstractions.Refresh;
using Application.Common.Security.Authentication.Tokens.Abstractions.Rotation;
using Application.Common.Security.Authentication.Tokens.Options;

using Infrastructure.Security.Authentication;
using Infrastructure.Security.Authentication.Contexts;
using Application.Common.Persistence.UnitOfWorks;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;
using Infrastructure.Security.Authentication.Tokens.Services;
using Infrastructure.Security.Identity.Domain.Users;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;

        builder.AddAuthentication(configuration);

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

    [Fact(DisplayName = "AddAuthentication should resolve the complete token and Identity pipeline")]
    public void AddAuthentication_ShouldResolveFullPipeline()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"Auth_{Guid.NewGuid():N}"));
        services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
        builder.AddAuthentication(BuildConfiguration());

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IRefreshTokenService>().Should().NotBeNull();
        provider.GetRequiredService<ITokenBlacklist>().Should().NotBeNull();
        provider.GetRequiredService<ITokenRotationService>().Should().NotBeNull();
        provider.GetRequiredService<UserManager<User>>().Should().NotBeNull();
    }

    [Fact(DisplayName = "AddAuthentication should reject null dependencies")]
    public void AddAuthentication_ShouldRejectNullDependencies()
    {
        IConfiguration configuration = BuildConfiguration();
        WebApplicationBuilder? builder = null;

        Assert.Throws<ArgumentNullException>(() => builder!.AddAuthentication(configuration));
        Assert.Throws<ArgumentNullException>(
            () => WebApplication.CreateBuilder().AddAuthentication((IConfiguration)null!));
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
