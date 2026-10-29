using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Application.Common.Security.Authorization;
using Application.Common.Security.Authorization.Options;
using Application.Common.Security.Authorization.Permissions.Caches;
using Application.Common.Security.Authorization.Permissions.Services;
using Application.Common.Security.Authorization.Policies;
using Application.Common.Security.Authorization.Requirements;

namespace Application.Unit.Tests.Common.Security.Authorization;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "AuthorizationComposition")]
public sealed class AuthorizationExtensionTests
{
    [Fact(DisplayName = "AddApplicationAuthorization: should register the authorization pipeline")]
    public void AddApplicationAuthorization_ShouldRegisterPipeline()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        WebApplicationBuilder returned = builder.AddApplicationAuthorization();

        returned.Should().BeSameAs(builder);
        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IPermissionCache) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IPermissionService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IAuthorizationHandler) &&
            descriptor.ImplementationType == typeof(PermissionRequirementAuthorizationHandler));
    }

    [Fact(DisplayName = "AddApplicationAuthorization: should replace the policy provider")]
    public void AddApplicationAuthorization_ShouldRegisterPolicyProvider()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.AddApplicationAuthorization();

        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IAuthorizationPolicyProvider) &&
            descriptor.ImplementationType == typeof(PermissionPolicyProvider));
    }

    [Fact(DisplayName = "AddApplicationAuthorization: should bind and validate authorization options")]
    public void AddApplicationAuthorization_ShouldBindOptions()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration["Authorization:PermissionCache:SlidingExpiration"] = "00:10:00";
        builder.Configuration["Authorization:PermissionCache:AbsoluteExpiration"] = "01:00:00";

        builder.AddApplicationAuthorization();
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        AuthzSetting setting = provider.GetRequiredService<IOptions<AuthzSetting>>().Value;

        setting.PermissionCache.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(10));
        setting.PermissionCache.AbsoluteExpiration.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact(DisplayName = "AddApplicationAuthorization: should throw when the builder is null")]
    public void AddApplicationAuthorization_WithNullBuilder_ShouldThrow()
    {
        WebApplicationBuilder? builder = null;

        Action action = () => builder!.AddApplicationAuthorization();

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "UseApplicationAuthorization: should add the middleware and return the app")]
    public void UseApplicationAuthorization_ShouldReturnSameApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddApplicationAuthorization();
        using WebApplication app = builder.Build();

        WebApplication returned = app.UseApplicationAuthorization();

        returned.Should().BeSameAs(app);
    }

    [Fact(DisplayName = "UseApplicationAuthorization: should throw when the application is null")]
    public void UseApplicationAuthorization_WithNullApplication_ShouldThrow()
    {
        WebApplication? app = null;

        Action action = () => app!.UseApplicationAuthorization();

        action.Should().Throw<ArgumentNullException>();
    }
}
