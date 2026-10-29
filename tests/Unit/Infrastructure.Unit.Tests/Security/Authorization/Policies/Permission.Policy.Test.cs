using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

using Application.Common.Security.Authorization;
using Application.Common.Security.Authorization.Policies;
using Application.Common.Security.Authorization.Registry;
using Application.Common.Security.Authorization.Requirements;

using Infrastructure.Security.Authorization.Policies;

namespace Infrastructure.Unit.Tests.Security.Authorization.Policies;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "PermissionPolicyProvider")]
public sealed class PermissionPolicyProviderTests
{
    private readonly AuthorizationOptions _options = new();
    private readonly PermissionPolicyProvider _sut;

    public PermissionPolicyProviderTests()
    {
        _sut = new PermissionPolicyProvider(Microsoft.Extensions.Options.Options.Create(_options));
    }

    [Fact(DisplayName = "PermissionPolicyProvider: should resolve a registered permission to a requirement")]
    public async Task GetPolicyAsync_ShouldReturnPermissionRequirement_ForRegisteredPermission()
    {
        string identifier = PermissionContext.Identity.Users.Manage.Identifier;

        AuthorizationPolicy? policy = await _sut.GetPolicyAsync(identifier);

        policy.Should().NotBeNull();
        policy!.Requirements.Should().Contain(requirement =>
            requirement is DenyAnonymousAuthorizationRequirement);
        PermissionRequirement permissionRequirement =
            policy.Requirements.OfType<PermissionRequirement>().Single();
        permissionRequirement.Permission.Should().Be(identifier);
    }

    [Fact(DisplayName = "PermissionPolicyProvider: should resolve any canonical identifier so new permissions need no code change")]
    public async Task GetPolicyAsync_ShouldReturnPermissionRequirement_ForUnregisteredCanonicalIdentifier()
    {
        const string identifier = "catalog.products.product.publish";

        AuthorizationPolicy? policy = await _sut.GetPolicyAsync(identifier);

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>().Single()
            .Permission.Should().Be(identifier);
    }

    [Fact(DisplayName = "PermissionPolicyProvider: should normalize the policy name")]
    public async Task GetPolicyAsync_ShouldNormalizePolicyName()
    {
        AuthorizationPolicy? policy = await _sut.GetPolicyAsync("  Catalog.Products.Product.Publish  ");

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>().Single()
            .Permission.Should().Be("catalog.products.product.publish");
    }

    [Fact(DisplayName = "PermissionPolicyProvider: should delegate non-permission names to the default provider")]
    public async Task GetPolicyAsync_ShouldReturnNull_ForNonPermissionName()
    {
        AuthorizationPolicy? policy = await _sut.GetPolicyAsync("unknown.permission");

        policy.Should().BeNull();
    }

    [Fact(DisplayName = "PermissionPolicyProvider: should return configured named policies through the fallback")]
    public async Task GetPolicyAsync_ShouldReturnConfiguredNamedPolicy()
    {
        var configured = new AuthorizationPolicyBuilder().RequireClaim("scope", "admin").Build();
        _options.AddPolicy("AdminOnly", configured);

        AuthorizationPolicy? policy = await _sut.GetPolicyAsync("AdminOnly");

        policy.Should().BeSameAs(configured);
    }

    [Fact(DisplayName = "PermissionPolicyProvider: configured policies should win over permission identifiers")]
    public async Task GetPolicyAsync_ShouldPreferConfiguredPolicy_OverPermissionIdentifier()
    {
        const string identifier = "catalog.products.product.publish";
        var configured = new AuthorizationPolicyBuilder().RequireClaim("scope", "publisher").Build();
        _options.AddPolicy(identifier, configured);

        AuthorizationPolicy? policy = await _sut.GetPolicyAsync(identifier);

        policy.Should().BeSameAs(configured);
        policy!.Requirements.Should().NotContain(requirement =>
            requirement is PermissionRequirement);
    }

    [Fact(DisplayName = "PermissionPolicyProvider: should return null for a blank policy name")]
    public async Task GetPolicyAsync_WithBlankName_ShouldReturnNull()
    {
        (await _sut.GetPolicyAsync(string.Empty)).Should().BeNull();
        (await _sut.GetPolicyAsync("   ")).Should().BeNull();
        (await _sut.GetPolicyAsync(null!)).Should().BeNull();
    }

    [Fact(DisplayName = "PermissionPolicyProvider: should resolve a multi-permission ALL policy")]
    public async Task GetPolicyAsync_ShouldResolveAllPolicy()
    {
        string policyName = PermissionPolicyParser.Compose(
            ["identity.users.all.manage", "identity.roles.all.read"],
            PermissionMatchMode.All);

        AuthorizationPolicy? policy = await _sut.GetPolicyAsync(policyName);

        PermissionRequirement requirement = policy!.Requirements
            .OfType<PermissionRequirement>()
            .Single();
        requirement.MatchMode.Should().Be(PermissionMatchMode.All);
        requirement.Permissions.Should().BeEquivalentTo(
            ["identity.users.all.manage", "identity.roles.all.read"]);
    }

    [Fact(DisplayName = "PermissionPolicyProvider: should resolve a multi-permission ANY policy")]
    public async Task GetPolicyAsync_ShouldResolveAnyPolicy()
    {
        string policyName = PermissionPolicyParser.Compose(
            ["identity.users.all.manage", "identity.roles.all.read"],
            PermissionMatchMode.Any);

        AuthorizationPolicy? policy = await _sut.GetPolicyAsync(policyName);

        PermissionRequirement requirement = policy!.Requirements
            .OfType<PermissionRequirement>()
            .Single();
        requirement.MatchMode.Should().Be(PermissionMatchMode.Any);
        requirement.Permissions.Should().HaveCount(2);
    }

    [Fact(DisplayName = "PermissionPolicyProvider: malformed encoded policies should return null")]
    public async Task GetPolicyAsync_WithMalformedEncodedPolicy_ShouldReturnNull()
    {
        (await _sut.GetPolicyAsync("permission:all")).Should().BeNull();
        (await _sut.GetPolicyAsync("permission:some:a.b.c.d")).Should().BeNull();
        (await _sut.GetPolicyAsync("permission:all:not-a-permission")).Should().BeNull();
        (await _sut.GetPolicyAsync("permission:any:")).Should().BeNull();
    }

    [Fact(DisplayName = "PermissionPolicyProvider: should delegate the default policy")]
    public async Task GetDefaultPolicyAsync_ShouldDelegateToDefaultProvider()
    {
        AuthorizationPolicy policy = await _sut.GetDefaultPolicyAsync();

        policy.Should().NotBeNull();
    }

    [Fact(DisplayName = "PermissionPolicyProvider: should delegate the fallback policy")]
    public async Task GetFallbackPolicyAsync_ShouldDelegateToDefaultProvider()
    {
        AuthorizationPolicy? policy = await _sut.GetFallbackPolicyAsync();

        policy.Should().BeNull();

        _options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

        (await _sut.GetFallbackPolicyAsync()).Should().BeSameAs(_options.FallbackPolicy);
    }
}
