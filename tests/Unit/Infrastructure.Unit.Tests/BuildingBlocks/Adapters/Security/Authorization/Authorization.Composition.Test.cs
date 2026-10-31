using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Application.BuildingBlocks.Security.Authorization.Options;
using Application.BuildingBlocks.Security.Authorization.Permissions.Caches;
using Application.BuildingBlocks.Security.Authorization.Permissions.Services;

using Infrastructure.BuildingBlocks.Adapters.Security.Authorization;
using Infrastructure.BuildingBlocks.Adapters.Security.Authorization.Policies;
using Infrastructure.BuildingBlocks.Adapters.Security.Authorization.Requirements;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Authorization;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "AuthorizationComposition")]
public sealed class AuthorizationPipelineCompositionTests
{
    [Fact(DisplayName = "AddAuthorizationPipeline: should register the authorization pipeline")]
    public void AddAuthorizationPipeline_ShouldRegisterPipeline()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        WebApplicationBuilder returned = builder.AddAuthorizationPipeline();

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

    [Fact(DisplayName = "AddAuthorizationPipeline: should replace the policy provider")]
    public void AddAuthorizationPipeline_ShouldRegisterPolicyProvider()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.AddAuthorizationPipeline();

        builder.Services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IAuthorizationPolicyProvider) &&
            descriptor.ImplementationType == typeof(PermissionPolicyProvider));
    }

    [Fact(DisplayName = "AddAuthorizationPipeline: should bind and validate authorization options")]
    public void AddAuthorizationPipeline_ShouldBindOptions()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration["Authorization:PermissionCache:SlidingExpiration"] = "00:10:00";
        builder.Configuration["Authorization:PermissionCache:AbsoluteExpiration"] = "01:00:00";

        builder.AddAuthorizationPipeline();
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        AuthzSetting setting = provider.GetRequiredService<IOptions<AuthzSetting>>().Value;

        setting.PermissionCache.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(10));
        setting.PermissionCache.AbsoluteExpiration.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact(DisplayName = "AddAuthorizationPipeline: should throw when the builder is null")]
    public void AddAuthorizationPipeline_WithNullBuilder_ShouldThrow()
    {
        WebApplicationBuilder? builder = null;

        Action action = () => builder!.AddAuthorizationPipeline();

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "UseAuthorizationPipeline: should add the middleware and return the app")]
    public void UseAuthorizationPipeline_ShouldReturnSameApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddAuthorizationPipeline();
        using WebApplication app = builder.Build();

        WebApplication returned = app.UseAuthorizationPipeline();

        returned.Should().BeSameAs(app);
    }

    [Fact(DisplayName = "UseAuthorizationPipeline: should throw when the application is null")]
    public void UseAuthorizationPipeline_WithNullApplication_ShouldThrow()
    {
        WebApplication? app = null;

        Action action = () => app!.UseAuthorizationPipeline();

        action.Should().Throw<ArgumentNullException>();
    }
}
