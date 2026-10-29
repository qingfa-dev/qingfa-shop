using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using Application.Common.Security.Authorization;
using Application.Common.Security.Authorization.Permissions.Services;
using Application.Common.Security.Authorization.Requirements;

using Infrastructure.Security.Authorization.Requirements;

namespace Infrastructure.Unit.Tests.Security.Authorization.Requirements;

[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "PermissionRequirementAuthorizationHandler")]
public sealed class PermissionRequirementAuthorizationHandlerTests
{
    private readonly Mock<IPermissionService> _permissionServiceMock;
    private readonly Mock<ILogger<PermissionRequirementAuthorizationHandler>> _loggerMock;
    private readonly PermissionRequirementAuthorizationHandler _sut;

    public PermissionRequirementAuthorizationHandlerTests()
    {
        _permissionServiceMock = new Mock<IPermissionService>();
        _loggerMock = new Mock<ILogger<PermissionRequirementAuthorizationHandler>>();

        _sut = new PermissionRequirementAuthorizationHandler(
            _permissionServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: Should succeed immediately for admin user, bypassing permission service")]
    public async Task HandleRequirementAsync_ShouldSucceedImmediately_WhenUserIsAdmin()
    {
        PermissionRequirement requirement = new("any.permission");
        ClaimsPrincipal principal = CreatePrincipal(nameId: Guid.NewGuid().ToString(), isAdmin: true);
        AuthorizationHandlerContext context = new([requirement], principal, null);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        _permissionServiceMock.Verify(
            x => x.GetEffectiveUserPermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: Should succeed when non-admin user has the required permission")]
    public async Task HandleRequirementAsync_ShouldSucceed_WhenNonAdminUserHasPermission()
    {
        Guid userId = Guid.NewGuid();
        PermissionRequirement requirement = new("any.permission");
        ClaimsPrincipal principal = CreatePrincipal(nameId: userId.ToString(), isAdmin: false);
        AuthorizationHandlerContext context = new([requirement], principal, null);

        _permissionServiceMock
            .Setup(x => x.GetEffectiveUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok(new HashSet<string> { "any.permission" }));

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        _permissionServiceMock.Verify(
            x => x.GetEffectiveUserPermissionsAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: Should fail when non-admin user lacks the required permission")]
    public async Task HandleRequirementAsync_ShouldFail_WhenNonAdminUserLacksPermission()
    {
        Guid userId = Guid.NewGuid();
        PermissionRequirement requirement = new("any.permission");
        ClaimsPrincipal principal = CreatePrincipal(nameId: userId.ToString(), isAdmin: false);
        AuthorizationHandlerContext context = new([requirement], principal, null);

        _permissionServiceMock
            .Setup(x => x.GetEffectiveUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok(new HashSet<string>()));

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: Should not succeed for unauthenticated user")]
    public async Task HandleRequirementAsync_ShouldNotSucceed_WhenUserIsUnauthenticated()
    {
        PermissionRequirement requirement = new("any.permission");
        ClaimsPrincipal principal = new(new ClaimsIdentity());
        AuthorizationHandlerContext context = new([requirement], principal, null);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        _permissionServiceMock.Verify(
            x => x.GetEffectiveUserPermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: Should not succeed when authenticated user has no NameIdentifier claim")]
    public async Task HandleRequirementAsync_ShouldNotSucceed_WhenUserHasNoNameIdentifier()
    {
        PermissionRequirement requirement = new("any.permission");
        List<Claim> claims = [new Claim(ClaimTypes.Name, "testuser")];
        ClaimsIdentity identity = new(claims, "test");
        ClaimsPrincipal principal = new(identity);
        AuthorizationHandlerContext context = new([requirement], principal, null);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: Should succeed for admin user even when permission service would fail")]
    public async Task HandleRequirementAsync_ShouldSucceed_WhenAdminUserRegardlessOfServiceState()
    {
        PermissionRequirement requirement = new("any.permission");
        ClaimsPrincipal principal = CreatePrincipal(nameId: Guid.NewGuid().ToString(), isAdmin: true);
        AuthorizationHandlerContext context = new([requirement], principal, null);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: Should resolve the sub claim when nameidentifier is absent")]
    public async Task HandleRequirementAsync_ShouldUseSubClaim_WhenNameIdentifierIsAbsent()
    {
        Guid userId = Guid.NewGuid();
        PermissionRequirement requirement = new("any.permission");
        ClaimsIdentity identity = new([new Claim("sub", userId.ToString())], "test");
        AuthorizationHandlerContext context = new([requirement], new ClaimsPrincipal(identity), null);

        _permissionServiceMock
            .Setup(x => x.GetEffectiveUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok(["any.permission"]));

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: Should deny when the identifier is not a Guid")]
    public async Task HandleRequirementAsync_ShouldDeny_WhenIdentifierIsNotGuid()
    {
        PermissionRequirement requirement = new("any.permission");
        AuthorizationHandlerContext context = new(
            [requirement],
            CreatePrincipal(nameId: "not-a-guid"),
            null);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        _permissionServiceMock.Verify(
            x => x.GetEffectiveUserPermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: Should deny when permission resolution fails")]
    public async Task HandleRequirementAsync_ShouldDeny_WhenResolutionFails()
    {
        Guid userId = Guid.NewGuid();
        PermissionRequirement requirement = new("any.permission");
        AuthorizationHandlerContext context = new(
            [requirement],
            CreatePrincipal(nameId: userId.ToString()),
            null);

        _permissionServiceMock
            .Setup(x => x.GetEffectiveUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Fail(
                Error.Unexpected("Resolve.Fail", "Store unavailable.")));

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: Should require every permission in ALL mode")]
    public async Task HandleRequirementAsync_AllMode_ShouldRequireEveryPermission()
    {
        Guid userId = Guid.NewGuid();
        PermissionRequirement requirement = new(
            ["a.b.c.read", "a.b.c.write"],
            PermissionMatchMode.All);
        AuthorizationHandlerContext context = new(
            [requirement],
            CreatePrincipal(nameId: userId.ToString()),
            null);

        _permissionServiceMock
            .Setup(x => x.GetEffectiveUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok(["a.b.c.read", "a.b.c.write"]));

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: ALL mode should deny when one permission is missing")]
    public async Task HandleRequirementAsync_AllMode_ShouldDenyWhenOneMissing()
    {
        Guid userId = Guid.NewGuid();
        PermissionRequirement requirement = new(
            ["a.b.c.read", "a.b.c.write"],
            PermissionMatchMode.All);
        AuthorizationHandlerContext context = new(
            [requirement],
            CreatePrincipal(nameId: userId.ToString()),
            null);

        _permissionServiceMock
            .Setup(x => x.GetEffectiveUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok(["a.b.c.read"]));

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: ANY mode should succeed on one permission")]
    public async Task HandleRequirementAsync_AnyMode_ShouldSucceedOnOnePermission()
    {
        Guid userId = Guid.NewGuid();
        PermissionRequirement requirement = new(
            ["a.b.c.read", "a.b.c.write"],
            PermissionMatchMode.Any);
        AuthorizationHandlerContext context = new(
            [requirement],
            CreatePrincipal(nameId: userId.ToString()),
            null);

        _permissionServiceMock
            .Setup(x => x.GetEffectiveUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok(["a.b.c.write"]));

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: ANY mode should deny when none are held")]
    public async Task HandleRequirementAsync_AnyMode_ShouldDenyWhenNoneHeld()
    {
        Guid userId = Guid.NewGuid();
        PermissionRequirement requirement = new(
            ["a.b.c.read", "a.b.c.write"],
            PermissionMatchMode.Any);
        AuthorizationHandlerContext context = new(
            [requirement],
            CreatePrincipal(nameId: userId.ToString()),
            null);

        _permissionServiceMock
            .Setup(x => x.GetEffectiveUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok(["a.b.c.delete"]));

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: Should deny an empty permission list")]
    public async Task HandleRequirementAsync_EmptyList_ShouldDeny()
    {
        PermissionRequirement requirement = new([], PermissionMatchMode.All);
        AuthorizationHandlerContext context = new(
            [requirement],
            CreatePrincipal(nameId: Guid.NewGuid().ToString()),
            null);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        _permissionServiceMock.Verify(
            x => x.GetEffectiveUserPermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact(DisplayName = "PermissionRequirementAuthorizationHandler: Should deny an empty requirement")]
    public async Task HandleRequirementAsync_ShouldDeny_WhenRequirementIsEmpty()
    {
        PermissionRequirement requirement = new("   ");
        AuthorizationHandlerContext context = new(
            [requirement],
            CreatePrincipal(nameId: Guid.NewGuid().ToString()),
            null);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        _permissionServiceMock.Verify(
            x => x.GetEffectiveUserPermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    private static ClaimsPrincipal CreatePrincipal(string? nameId = null, bool isAdmin = false)
    {
        List<Claim> claims = [];
        if (nameId != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, nameId));
        if (isAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        ClaimsIdentity identity = new(claims, "test");
        return new ClaimsPrincipal(identity);
    }
}
