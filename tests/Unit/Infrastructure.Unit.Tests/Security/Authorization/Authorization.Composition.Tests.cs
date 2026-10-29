using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Application.Common.Security.Authorization.Permissions.Store;
using Application.Common.Security.Authorization.Registry;

using Infrastructure.Security.Authorization;
using Infrastructure.Security.Authorization.Permissions.Registry;
using Infrastructure.Security.Authorization.Permissions.Store;

namespace Infrastructure.Unit.Tests.Security.Authorization;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "AuthorizationComposition")]
public sealed class AuthorizationCompositionTests
{
    [Fact(DisplayName = "AddAuthorizationStore: should register the EF Core permission store")]
    public void AddAuthorizationStore_ShouldRegisterPermissionStore()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;

        WebApplicationBuilder returned = builder.AddAuthorizationStore();

        returned.Should().BeSameAs(builder);
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IPermissionStore) &&
            descriptor.ImplementationType == typeof(PermissionStoreService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact(DisplayName = "AddAuthorizationStore: should discover the metadata registry as a singleton")]
    public void AddAuthorizationStore_ShouldRegisterMetadataRegistry()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;

        builder.AddAuthorizationStore();

        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IPermissionMetadataRegistry) &&
            descriptor.ImplementationType == typeof(PermissionMetadataRegistry) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(TimeProvider));
    }

    [Fact(DisplayName = "AddAuthorizationStore: should resolve the metadata registry")]
    public void AddAuthorizationStore_ShouldResolveMetadataRegistry()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        services.AddLogging();
        builder.AddAuthorizationStore();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IPermissionMetadataRegistry>()
            .Should().BeOfType<PermissionMetadataRegistry>();
    }

    [Fact(DisplayName = "AddAuthorizationStore: should not replace an existing permission store")]
    public void AddAuthorizationStore_ShouldNotReplaceExistingRegistration()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        services.AddSingleton<IPermissionStore>(Mock.Of<IPermissionStore>());

        builder.AddAuthorizationStore();

        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IPermissionStore));
    }

    [Fact(DisplayName = "AddAuthorizationStore: should throw when the builder is null")]
    public void AddAuthorizationStore_WithNullBuilder_ShouldThrow()
    {
        WebApplicationBuilder? builder = null;

        Action action = () => builder!.AddAuthorizationStore();

        action.Should().Throw<ArgumentNullException>();
    }
}
