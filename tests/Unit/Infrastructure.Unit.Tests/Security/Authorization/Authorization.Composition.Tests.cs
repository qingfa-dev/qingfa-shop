using Microsoft.Extensions.DependencyInjection;

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
        var services = new ServiceCollection();

        IServiceCollection returned = services.AddAuthorizationStore();

        returned.Should().BeSameAs(services);
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IPermissionStore) &&
            descriptor.ImplementationType == typeof(PermissionStoreService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact(DisplayName = "AddAuthorizationStore: should discover the metadata registry as a singleton")]
    public void AddAuthorizationStore_ShouldRegisterMetadataRegistry()
    {
        var services = new ServiceCollection();

        services.AddAuthorizationStore();

        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IPermissionMetadataRegistry) &&
            descriptor.ImplementationType == typeof(PermissionMetadataRegistry) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(TimeProvider));
    }

    [Fact(DisplayName = "AddAuthorizationStore: should not replace an existing permission store")]
    public void AddAuthorizationStore_ShouldNotReplaceExistingRegistration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPermissionStore>(Mock.Of<IPermissionStore>());

        services.AddAuthorizationStore();

        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IPermissionStore));
    }

    [Fact(DisplayName = "AddAuthorizationStore: should throw when services is null")]
    public void AddAuthorizationStore_WithNullServices_ShouldThrow()
    {
        IServiceCollection? services = null;

        Action action = () => services!.AddAuthorizationStore();

        action.Should().Throw<ArgumentNullException>();
    }
}
