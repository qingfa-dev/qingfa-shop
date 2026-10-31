using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Registry;
using Application.BuildingBlocks.Security.Identity.Permissions.Mappings;
using Application.BuildingBlocks.Security.Identity.Permissions.Models;

namespace Application.Unit.Tests.BuildingBlocks.Security.Identity.Permissions;

/// <summary>Unit tests for the permission model mapper.</summary>
public sealed class PermissionMapperTest
{
    [Fact(DisplayName = "Metadata should map every catalog field")]
    public void ToModel_ShouldMapEveryField()
    {
        PermissionMetadata metadata = PermissionMetadata.Create(
            domain: "catalog",
            category: "products",
            resource: "product",
            action: "publish",
            name: "Publish products",
            description: "Publishes catalog products.");

        PermissionItemModel model = metadata.ToModel();

        model.Identifier.Should().Be("catalog.products.product.publish");
        model.Domain.Should().Be("catalog");
        model.Category.Should().Be("products");
        model.Resource.Should().Be("product");
        model.Action.Should().Be("publish");
        model.Description.Should().Be("Publishes catalog products.");
    }

    [Fact(DisplayName = "Snapshot should map every permission and the refresh instant")]
    public void Snapshot_ShouldMapPermissionsAndInstant()
    {
        PermissionMetadataSnapshot snapshot = PermissionMetadataSnapshot.Create(
            [PermissionContext.Identity.Users.Manage, PermissionContext.Identity.Roles.Read],
            PermissionFixture.RefreshedOnUtc);

        PermissionSnapshotModel model = snapshot.ToModel();

        model.Permissions.Should().HaveCount(2);
        model.Permissions.Select(permission => permission.Identifier)
            .Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
        model.RefreshedOnUtc.Should().Be(PermissionFixture.RefreshedOnUtc);
    }

    [Fact(DisplayName = "Snapshot should page with defaults")]
    public void ToPage_ShouldApplyDefaults()
    {
        Result<QueryPage<PermissionItemModel>> result = PermissionFixture.Snapshot.ToPage(new QueryParameters());
        QueryPage<PermissionItemModel> page = result.Value;

        page.Page.Should().Be(1);
        page.PageSize.Should().Be(20);
        page.TotalCount.Should().Be(3);
        page.Items.Should().HaveCount(3);
        page.HasPrevious.Should().BeFalse();
        page.HasNext.Should().BeFalse();
    }

    [Fact(DisplayName = "Snapshot should reject a page size above the shared maximum")]
    public void ToPage_ShouldRejectOversizedPage()
    {
        Result<QueryPage<PermissionItemModel>> result = PermissionFixture.Snapshot.ToPage(
            new QueryParameters { Page = 2, PageSize = 500 });

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(QueryOutcome.Codes.PageSizeInvalid);
    }

    [Fact(DisplayName = "Snapshot should reject a non-positive page")]
    public void ToPage_WhenPageIsNonPositive_ShouldFail()
    {
        Result<QueryPage<PermissionItemModel>> result = PermissionFixture.Snapshot.ToPage(
            new QueryParameters { Page = 0 });

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(QueryOutcome.Codes.PageInvalid);
    }

    [Fact(DisplayName = "Snapshot should return an empty page past the end")]
    public void ToPage_WhenPageIsPastTheEnd_ShouldReturnEmpty()
    {
        Result<QueryPage<PermissionItemModel>> result = PermissionFixture.Snapshot.ToPage(
            new QueryParameters { Page = int.MaxValue, PageSize = 1 });
        QueryPage<PermissionItemModel> page = result.Value;

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(3);
    }
}
