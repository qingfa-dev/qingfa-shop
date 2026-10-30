using Application.Features.Identity.Permissions.Commands;
using Application.Features.Identity.Permissions.Models;

namespace Application.Unit.Tests.Features.Identity.Permissions.Commands;

/// <summary>Unit tests for the permission command handlers.</summary>
public sealed class PermissionCommandsTest
{
    [Fact(DisplayName = "Refresh should return the rebuilt snapshot")]
    public async Task Refresh_ShouldReturnSnapshot()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.RefreshAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionSnapshotModel>.Ok(PermissionFixture.Snapshot));

        Result<PermissionSnapshotModel> result =
            await new RefreshPermissionCatalog.UseCaseHandler(fixture.Catalog.Object)
                .Handle(new RefreshPermissionCatalog.UseCase(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Permissions.Should().HaveCount(3);
        result.Value.RefreshedOnUtc.Should().Be(PermissionFixture.RefreshedOnUtc);
    }

    [Fact(DisplayName = "Refresh should preserve a service failure")]
    public async Task Refresh_WhenServiceFails_ShouldPreserveFailure()
    {
        var fixture = new PermissionFixture();
        fixture.Catalog
            .Setup(service => service.RefreshAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionSnapshotModel>.Fail(PermissionFixture.Failure));

        Result<PermissionSnapshotModel> result =
            await new RefreshPermissionCatalog.UseCaseHandler(fixture.Catalog.Object)
                .Handle(new RefreshPermissionCatalog.UseCase(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Permission.Test.Unavailable");
    }
}
