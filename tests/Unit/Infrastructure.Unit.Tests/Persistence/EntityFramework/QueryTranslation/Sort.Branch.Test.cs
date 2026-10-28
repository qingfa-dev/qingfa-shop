using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Sorting;

using Infrastructure.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.QueryTranslation;

/// <summary>Edge cases for ordering, null-state ordering, and paging.</summary>
[Trait("Category", "Unit")]
public sealed class SortBranchTest
{
    private static readonly QueryMap<BranchProbe> Map = BranchProbeMap.Create();
    private static readonly IQueryTranslator<BranchProbe> Translator = new QueryTranslationEfCore<BranchProbe>();

    [Theory(DisplayName = "Nullable value null-state ordering should match the SortNulls contract")]
    [InlineData(SortNulls.HasValue, new[] { 1, 2, 0 })]
    [InlineData(SortNulls.NotHasValue, new[] { 0, 1, 2 })]
    public void ApplySort_WithNullableValueNullState_ShouldOrderNulls(
        SortNulls nulls,
        int[] expected)
    {
        var rows = new List<BranchProbe>
        {
            new() { Id = Guid.NewGuid(), NullableScore = 2 },
            new() { Id = Guid.NewGuid(), NullableScore = null },
            new() { Id = Guid.NewGuid(), NullableScore = 1 },
        }.AsQueryable();
        SortField[] order = [new("nullableScore", SortDirection.Ascending, nulls)];

        Result<IQueryable<BranchProbe>> result = Translator.ApplySort(rows, order, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(row => row.NullableScore ?? 0).Should().Equal(expected);
    }

    [Theory(DisplayName = "Reference null-state ordering should match the SortNulls contract")]
    [InlineData(SortNulls.HasValue, new[] { "alpha", "bravo", null })]
    [InlineData(SortNulls.NotHasValue, new[] { null, "alpha", "bravo" })]
    public void ApplySort_WithReferenceNullState_ShouldOrderNulls(
        SortNulls nulls,
        string?[] expected)
    {
        var rows = new List<BranchProbe>
        {
            new() { Id = Guid.NewGuid(), Name = "bravo" },
            new() { Id = Guid.NewGuid(), Name = null },
            new() { Id = Guid.NewGuid(), Name = "alpha" },
        }.AsQueryable();
        SortField[] order = [new("name", SortDirection.Ascending, nulls)];

        Result<IQueryable<BranchProbe>> result = Translator.ApplySort(rows, order, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(row => row.Name).Should().Equal(expected);
    }

    [Theory(DisplayName = "Nested null-state ordering should treat a missing navigation as null")]
    [InlineData(SortNulls.HasValue, new[] { "alpha", "bravo", null, null })]
    [InlineData(SortNulls.NotHasValue, new[] { null, null, "alpha", "bravo" })]
    public void ApplySort_WithNestedNullState_ShouldOrderNulls(
        SortNulls nulls,
        string?[] expected)
    {
        var rows = new List<BranchProbe>
        {
            new() { Id = Guid.NewGuid(), Child = new BranchChild { Name = "bravo" } },
            new() { Id = Guid.NewGuid(), Child = null },
            new() { Id = Guid.NewGuid(), Child = new BranchChild { Name = null } },
            new() { Id = Guid.NewGuid(), Child = new BranchChild { Name = "alpha" } },
        }.AsQueryable();
        SortField[] order = [new("child.name", SortDirection.Ascending, nulls)];

        Result<IQueryable<BranchProbe>> result = Translator.ApplySort(rows, order, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(row => row.Child == null ? null : row.Child.Name).Should().Equal(expected);
    }

    [Fact(DisplayName = "Sorting a collection field should be rejected")]
    public void ApplySort_WithCollectionField_ShouldFail()
    {
        SortField[] order = [new("tags", SortDirection.Ascending, SortNulls.None)];

        Result<IQueryable<BranchProbe>> result = Translator.ApplySort(
            new List<BranchProbe>().AsQueryable(), order, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotSortable);
    }

    [Fact(DisplayName = "Sorting an indexed field without an index should fail")]
    public void ApplySort_WithIndexedField_ShouldFail()
    {
        var builder = QueryMapBuilder<BranchProbe>.Create()
            .Map("indexed", probe => probe.NullableScore, field => field
                .Sortable()
                .IsIndexed(true));
        QueryMap<BranchProbe> map = builder.Build().Value;
        SortField[] order = [new("indexed", SortDirection.Ascending, SortNulls.None)];

        Result<IQueryable<BranchProbe>> result = Translator.ApplySort(
            new List<BranchProbe>().AsQueryable(), order, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotFilterable);
    }

    [Theory(DisplayName = "Paging should reject an invalid page window")]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(int.MaxValue, 2)]
    public void ApplyPaging_WithInvalidWindow_ShouldFail(int page, int pageSize)
    {
        Result<IQueryable<BranchProbe>> result = Translator.ApplyPaging(
            new List<BranchProbe>().AsQueryable(),
            new PageRequest(page, pageSize));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FilterSyntax);
    }

    [Fact(DisplayName = "Paging should accept the largest safe offset")]
    public void ApplyPaging_WithLargestSafeOffset_ShouldSucceed()
    {
        Result<IQueryable<BranchProbe>> result = Translator.ApplyPaging(
            new List<BranchProbe>().AsQueryable(),
            new PageRequest(int.MaxValue / 2, 2));

        result.IsSuccess.Should().BeTrue();
    }
}
