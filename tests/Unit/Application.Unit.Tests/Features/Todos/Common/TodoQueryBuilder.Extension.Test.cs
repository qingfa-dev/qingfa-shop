using Application.Common.Querying;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Sorting;
using Application.Features.Todos.Common;
using Application.Features.Todos.Items.Models;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Common;

/// <summary>Unit tests for the Todo query builder scope extensions.</summary>
public sealed class TodoQueryBuilderExtensionTest
{
    private static QueryBuilder<TodoReadModel> CreateBuilder()
    {
        var fixture = new TodoHandlerFixture();
        fixture.RegisterTodoReadModelMap();
        fixture.Registry.TryGet<TodoReadModel>(out QueryMap<TodoReadModel>? map).Should().BeTrue(
            because: "the fixture registers the todo query map");
        return QueryBuilder<TodoReadModel>.Create(map!);
    }

    [Fact(DisplayName = "ForTenant should add a tenantId equality condition")]
    public void ForTenant_ShouldAddTenantCondition()
    {
        var result = CreateBuilder()
            .ForTenant(TodoHandlerFixture.TenantId)
            .Build();

        result.IsSuccess.Should().BeTrue();
        FilterCondition condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.Field.Should().Be("tenantId");
        condition.Operator.Should().Be(FilterOperator.Equal);
        condition.Value.Should().Be(TodoHandlerFixture.TenantId);
        condition.ValueType.Should().Be(typeof(Guid));
    }

    [Fact(DisplayName = "IncludeDeleted(false) should hide soft-deleted rows")]
    public void IncludeDeleted_WhenFalse_ShouldAddDeletionCondition()
    {
        var result = CreateBuilder()
            .IncludeDeleted(includeDeleted: false)
            .Build();

        result.IsSuccess.Should().BeTrue();
        FilterCondition condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.Field.Should().Be("isDeleted");
        condition.Operator.Should().Be(FilterOperator.Equal);
        condition.Value.Should().Be(false);
        condition.ValueType.Should().Be(typeof(bool));
    }

    [Fact(DisplayName = "IncludeDeleted(true) should not add a deletion condition")]
    public void IncludeDeleted_WhenTrue_ShouldAddNothing()
    {
        var result = CreateBuilder()
            .IncludeDeleted()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeNull();
    }

    [Fact(DisplayName = "DefaultOrderBy should add createdAtUtc descending when no ordering exists")]
    public void DefaultOrderBy_WithoutOrdering_ShouldAddCreatedDescending()
    {
        var result = CreateBuilder()
            .DefaultOrderBy()
            .Build();

        result.IsSuccess.Should().BeTrue();
        SortField field = result.Value.OrderBy.Should().ContainSingle().Subject;
        field.Field.Should().Be("createdAtUtc");
        field.Direction.Should().Be(SortDirection.Descending);
        field.Nulls.Should().Be(SortNulls.None);
    }

    [Fact(DisplayName = "DefaultOrderBy should preserve caller ordering")]
    public void DefaultOrderBy_WithOrdering_ShouldNotOverride()
    {
        var result = CreateBuilder()
            .OrderBy(todo => todo.Title)
            .DefaultOrderBy()
            .Build();

        result.IsSuccess.Should().BeTrue();
        SortField field = result.Value.OrderBy.Should().ContainSingle().Subject;
        field.Field.Should().Be("title");
        field.Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact(DisplayName = "DefaultOrderBy should apply a caller-supplied field and direction")]
    public void DefaultOrderBy_WithCustomFieldAndDirection_ShouldUseThem()
    {
        var result = CreateBuilder()
            .DefaultOrderBy("tenantId", SortDirection.Ascending)
            .Build();

        result.IsSuccess.Should().BeTrue();
        SortField field = result.Value.OrderBy.Should().ContainSingle().Subject;
        field.Field.Should().Be("tenantId");
        field.Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact(DisplayName = "DefaultOrderBy should leave a failed builder unchanged")]
    public void DefaultOrderBy_WhenBuildFails_ShouldPreserveFailure()
    {
        var result = CreateBuilder()
            .Where("missing=1")
            .DefaultOrderBy()
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "ApplyTodoScope should combine tenant, deletion, and default ordering")]
    public void ApplyTodoScope_ShouldCombineAllScopes()
    {
        var result = CreateBuilder()
            .ApplyTodoScope(TodoHandlerFixture.TenantId, includeDeleted: false)
            .Build();

        result.IsSuccess.Should().BeTrue();
        FilterGroup group = result.Value.Filter.Should().BeOfType<FilterGroup>().Subject;
        group.Logic.Should().Be(FilterLogic.And);
        group.Children.OfType<FilterCondition>()
            .Should().Contain(condition =>
                condition.Field == "tenantId" &&
                Equals(condition.Value, TodoHandlerFixture.TenantId));
        group.Children.OfType<FilterCondition>()
            .Should().Contain(condition =>
                condition.Field == "isDeleted" &&
                Equals(condition.Value, false));
        SortField field = result.Value.OrderBy.Should().ContainSingle().Subject;
        field.Field.Should().Be("createdAtUtc");
        field.Direction.Should().Be(SortDirection.Descending);
    }

    [Fact(DisplayName = "ApplyTodoScope should omit the deletion condition when including deleted rows")]
    public void ApplyTodoScope_WhenIncludingDeleted_ShouldOnlyScopeTenant()
    {
        var result = CreateBuilder()
            .ApplyTodoScope(TodoHandlerFixture.TenantId, includeDeleted: true)
            .Build();

        result.IsSuccess.Should().BeTrue();
        FilterCondition condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.Field.Should().Be("tenantId");
        result.Value.OrderBy.Should().ContainSingle()
            .Which.Field.Should().Be("createdAtUtc");
    }

    [Fact(DisplayName = "ApplyTodoScope should keep the scope outside a later OrWhere")]
    public void ApplyTodoScope_WithOrWhere_ShouldKeepScopeOutsideTheOr()
    {
        var result = CreateBuilder()
            .ApplyTodoScope(TodoHandlerFixture.TenantId, includeDeleted: false)
            .Where(todo => todo.Title.Contains("a"))
            .OrWhere(todo => todo.Title.Contains("x"))
            .Build();

        result.IsSuccess.Should().BeTrue();
        var root = result.Value.Filter.Should().BeOfType<FilterGroup>().Subject;
        root.Logic.Should().Be(FilterLogic.And);
        root.Children.Should().HaveCount(2);

        var queryFilter = root.Children[0].Should().BeOfType<FilterGroup>().Subject;
        queryFilter.Logic.Should().Be(FilterLogic.Or);

        var scope = root.Children[1].Should().BeOfType<FilterGroup>().Subject;
        scope.Logic.Should().Be(FilterLogic.And);
        scope.Children.OfType<FilterCondition>()
            .Should().Contain(condition =>
                condition.Field == "tenantId" &&
                Equals(condition.Value, TodoHandlerFixture.TenantId));
        scope.Children.OfType<FilterCondition>()
            .Should().Contain(condition =>
                condition.Field == "isDeleted" &&
                Equals(condition.Value, false));
    }
}