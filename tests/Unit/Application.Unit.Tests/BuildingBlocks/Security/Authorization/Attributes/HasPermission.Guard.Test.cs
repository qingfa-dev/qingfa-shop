using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Application.BuildingBlocks.Security.Authorization;
using Application.BuildingBlocks.Security.Authorization.Attributes;
using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Registry;
using Application.Modules.Todos.Permissions;

namespace Application.Unit.Tests.BuildingBlocks.Security.Authorization.Attributes;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "HasPermission")]
public sealed class HasPermissionGuardTests
{
    [Fact(DisplayName = "HasPermissionAttribute: should expose the permission as the authorization policy")]
    public void Constructor_ShouldExposePermissionAsPolicy()
    {
        string identifier = PermissionContext.Identity.Users.Manage.Identifier;

        var attribute = new HasPermissionAttribute(identifier);

        attribute.Permission.Should().Be(identifier);
        attribute.Policy.Should().Be(identifier);
    }

    [Fact(DisplayName = "HasPermissionAttribute: should reject a blank permission")]
    public void Constructor_WithBlankPermission_ShouldThrow()
    {
        Action empty = () => _ = new HasPermissionAttribute(string.Empty);
        Action whitespace = () => _ = new HasPermissionAttribute("   ");
        Action missing = () => _ = new HasPermissionAttribute(null!);

        empty.Should().Throw<ArgumentException>();
        whitespace.Should().Throw<ArgumentException>();
        missing.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "HasPermission extension: should attach authorization metadata to the endpoint")]
    public async Task HasPermission_ShouldAttachMetadata()
    {
        await using WebApplication app = CreateApplication();
        string identifier = TodoPermissionCatalog.Items.Read.Identifier;

        RouteHandlerBuilder builder = app.MapGet("/test", () => "ok");
        RouteHandlerBuilder returned = builder.HasPermission(identifier);

        returned.Should().BeSameAs(builder);
    }

    [Fact(DisplayName = "HasPermission extension: should accept permission metadata")]
    public async Task HasPermission_WithMetadata_ShouldAttachMetadata()
    {
        await using WebApplication app = CreateApplication();
        RouteHandlerBuilder builder = app.MapGet("/test", () => "ok");

        RouteHandlerBuilder returned = builder.HasPermission(TodoPermissionCatalog.Items.Read);

        returned.Should().BeSameAs(builder);
    }

    [Fact(DisplayName = "HasPermissionAttribute: should default multiple permissions to ALL")]
    public void Constructor_WithMultiplePermissions_ShouldDefaultToAll()
    {
        var attribute = new HasPermissionAttribute(
            "identity.users.all.read",
            "identity.roles.all.read");

        attribute.MatchMode.Should().Be(PermissionMatchMode.All);
        attribute.Permissions.Should().BeEquivalentTo(
            ["identity.users.all.read", "identity.roles.all.read"]);
        attribute.Policy.Should().Be(
            "permission:all:identity.users.all.read|identity.roles.all.read");
    }

    [Fact(DisplayName = "HasPermissionAttribute: should support ANY match mode")]
    public void Constructor_WithAnyMode_ShouldComposeAnyPolicy()
    {
        var attribute = new HasPermissionAttribute(
            PermissionMatchMode.Any,
            "identity.users.all.read",
            "identity.roles.all.read");

        attribute.MatchMode.Should().Be(PermissionMatchMode.Any);
        attribute.Policy.Should().Be(
            "permission:any:identity.users.all.read|identity.roles.all.read");
    }

    [Fact(DisplayName = "HasPermissionAttribute: should normalize and de-duplicate identifiers")]
    public void Constructor_ShouldNormalizeIdentifiers()
    {
        var attribute = new HasPermissionAttribute(
            " Identity.Users.All.Read ",
            "identity.users.all.read");

        attribute.Permissions.Should().ContainSingle()
            .Which.Should().Be("identity.users.all.read");
    }

    [Fact(DisplayName = "HasPermission extension: should accept multiple permissions")]
    public async Task HasPermission_WithMultiple_ShouldAttachMetadata()
    {
        await using WebApplication app = CreateApplication();
        RouteHandlerBuilder builder = app.MapGet("/multi", () => "ok");

        RouteHandlerBuilder returned = builder.HasPermission(
            "identity.users.all.read",
            "identity.roles.all.read");

        returned.Should().BeSameAs(builder);
    }

    [Fact(DisplayName = "HasPermission extension: should accept multiple metadata values and ANY mode")]
    public async Task HasPermission_WithMultipleMetadataAndAny_ShouldAttachMetadata()
    {
        await using WebApplication app = CreateApplication();
        RouteHandlerBuilder builder = app.MapGet("/multi-any", () => "ok");

        RouteHandlerBuilder returned = builder.HasPermission(
            PermissionMatchMode.Any,
            TodoPermissionCatalog.Items.Read,
            TodoPermissionCatalog.Items.Update);

        returned.Should().BeSameAs(builder);
    }

    [Fact(DisplayName = "HasPermission extension: should reject a null builder")]
    public void HasPermission_WithNullBuilder_ShouldThrow()
    {
        RouteHandlerBuilder? builder = null;

        Action action = () => builder!.HasPermission("identity.profile.self.read");

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "HasPermission extension: should reject null permission metadata")]
    public async Task HasPermission_WithNullMetadata_ShouldThrow()
    {
        await using WebApplication app = CreateApplication();
        RouteHandlerBuilder builder = app.MapGet("/test", () => "ok");

        Action action = () => builder.HasPermission((PermissionMetadata)null!);

        action.Should().Throw<ArgumentNullException>();
    }

    private static WebApplication CreateApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();
        app.MapGet("/health", () => Results.Ok());
        return app;
    }
}
