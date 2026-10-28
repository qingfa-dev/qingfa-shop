using Contract.Common;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

namespace Contract.Unit.Tests.Common;

/// <summary>
/// Specification for <see cref="EntityMapperExtensions"/> create, update, and projection flows.
/// </summary>
[Trait("Category", "Unit")]
public class EntityMapperExtensionSpec
{
    #region Test Doubles

    private class TestParameters
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestResponse : TestParameters
    {
        public string Label { get; set; } = string.Empty;
    }

    private static Error Failure() =>
        new(code: "test.failure", message: "The operation failed.");

    private static TestEntity Entity(string name) =>
        new() { Name = name };

    #endregion

    #region Create

    [Fact]
    public void MapToDomain_Create_ShouldReturnCreatedEntity()
    {
        // Arrange:
        var parameters = new TestParameters { Name = "created" };

        // Act:
        var result = parameters.MapToDomain(
            input => Result<TestEntity>.Success(Entity(input.Name)));

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("created");
    }

    [Fact]
    public void MapToDomain_CreateFailure_ShouldPropagateFailure()
    {
        // Arrange:
        var parameters = new TestParameters();

        // Act:
        var result = parameters.MapToDomain(
            _ => Result<TestEntity>.Fail(Failure()));

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors.Single().Code.Should().Be("test.failure");
    }

    [Fact]
    public void MapToDomainAndForget_Create_ShouldDiscardEntityAndKeepSuccess()
    {
        // Arrange:
        var parameters = new TestParameters { Name = "forgotten" };
        var invocations = 0;

        // Act:
        var result = parameters.MapToDomainAndForget(input =>
        {
            invocations++;
            return Result<TestEntity>.Success(Entity(input.Name));
        });

        // Assert:
        result.IsSuccess.Should().BeTrue();
        invocations.Should().Be(1);
    }

    [Fact]
    public void MapToDomainAndForget_CreateFailure_ShouldPropagateFailure()
    {
        // Arrange:
        var parameters = new TestParameters();

        // Act:
        var result = parameters.MapToDomainAndForget(
            _ => Result<TestEntity>.Fail(Failure()));

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors.Single().Code.Should().Be("test.failure");
    }

    [Fact]
    public void MapToDomain_CreateWithFill_ShouldProjectCreatedEntity()
    {
        // Arrange:
        var parameters = new TestParameters { Name = "created" };

        // Act:
        var result = parameters.MapToDomain<TestEntity, TestParameters, TestResponse>(
            input => Result<TestEntity>.Success(Entity(input.Name)),
            (entity, response) =>
            {
                response.Name = entity.Name;
                response.Label = $"label:{entity.Name}";
            });

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<TestResponse>();
        result.Value.Name.Should().Be("created");
        result.Value.Label.Should().Be("label:created");
    }

    [Fact]
    public void MapToDomain_CreateFailure_ShouldNotFillResponse()
    {
        // Arrange:
        var parameters = new TestParameters();
        var filled = false;

        // Act:
        var result = parameters.MapToDomain<TestEntity, TestParameters, TestResponse>(
            _ => Result<TestEntity>.Fail(Failure()),
            (_, _) => filled = true);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors.Single().Code.Should().Be("test.failure");
        filled.Should().BeFalse();
    }

    #endregion

    #region Update

    [Fact]
    public void MapToDomain_Update_ShouldApplyParametersToEntity()
    {
        // Arrange:
        var entity = Entity("before");
        var parameters = new TestParameters { Name = "after" };

        // Act:
        var result = parameters.MapToDomain(
            entity,
            (target, input) =>
            {
                target.Name = input.Name;
                return Result.Success();
            });

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Name.Should().Be("after");
    }

    [Fact]
    public void MapToDomain_UpdateFailure_ShouldPropagateFailure()
    {
        // Arrange:
        var entity = Entity("unchanged");
        var parameters = new TestParameters { Name = "after" };

        // Act:
        var result = parameters.MapToDomain(
            entity,
            (_, _) => Result.Fail(Failure()));

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors.Single().Code.Should().Be("test.failure");
        entity.Name.Should().Be("unchanged");
    }

    [Fact]
    public void MapToDomainAndReturn_Update_ShouldReturnMutatedEntity()
    {
        // Arrange:
        var entity = Entity("before");
        var parameters = new TestParameters { Name = "after" };

        // Act:
        var result = parameters.MapToDomainAndReturn(
            entity,
            (target, input) =>
            {
                target.Name = input.Name;
                return Result.Success();
            });

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
        entity.Name.Should().Be("after");
    }

    [Fact]
    public void MapToDomainAndReturn_UpdateFailure_ShouldPropagateFailure()
    {
        // Arrange:
        var entity = Entity("unchanged");
        var parameters = new TestParameters();

        // Act:
        var result = parameters.MapToDomainAndReturn(
            entity,
            (_, _) => Result.Fail(Failure()));

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors.Single().Code.Should().Be("test.failure");
    }

    [Fact]
    public void MapToDomain_UpdateWithFill_ShouldProjectMutatedEntity()
    {
        // Arrange:
        var entity = Entity("before");
        var parameters = new TestParameters { Name = "after" };

        // Act:
        var result = parameters.MapToDomain<TestEntity, TestParameters, TestResponse>(
            entity,
            (target, input) =>
            {
                target.Name = input.Name;
                return Result.Success();
            },
            (source, response) =>
            {
                response.Name = source.Name;
                response.Label = "updated";
            });

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.Name.Should().Be("after");
        result.Value.Name.Should().Be("after");
        result.Value.Label.Should().Be("updated");
    }

    [Fact]
    public void MapToDomain_UpdateFailure_ShouldNotFillResponse()
    {
        // Arrange:
        var entity = Entity("unchanged");
        var parameters = new TestParameters();
        var filled = false;

        // Act:
        var result = parameters.MapToDomain<TestEntity, TestParameters, TestResponse>(
            entity,
            (_, _) => Result.Fail(Failure()),
            (_, _) => filled = true);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors.Single().Code.Should().Be("test.failure");
        filled.Should().BeFalse();
    }

    #endregion

    #region Project

    [Fact]
    public void MapToResponse_ShouldCreateAndFillResponse()
    {
        // Arrange:
        var entity = Entity("direct");
        var invocations = 0;

        // Act:
        var response = entity.MapToResponse<TestEntity, TestParameters, TestResponse>(
            (source, target) =>
            {
                invocations++;
                target.Name = source.Name;
                target.Label = "projected";
            });

        // Assert:
        invocations.Should().Be(1);
        response.Name.Should().Be("direct");
        response.Label.Should().Be("projected");
    }

    [Fact]
    public void MapToResponses_Enumerable_ShouldProjectEveryEntityInOrder()
    {
        // Arrange:
        var entities = new[] { Entity("first"), Entity("second") };

        // Act:
        var responses = entities.MapToResponses<TestEntity, TestParameters, TestResponse>(
            (source, target) =>
            {
                target.Name = source.Name;
                target.Label = "item";
            });

        // Assert:
        responses.Should().HaveCount(2);
        responses.Select(response => response.Name)
            .Should().ContainInOrder("first", "second");
        responses.Should().OnlyContain(response => response.Label == "item");
    }

    [Fact]
    public void MapToResponses_EmptyEnumerable_ShouldReturnEmptyList()
    {
        // Arrange:
        var entities = Array.Empty<TestEntity>();

        // Act:
        var responses = entities.MapToResponses<TestEntity, TestParameters, TestResponse>(
            (_, _) => { });

        // Assert:
        responses.Should().BeEmpty();
    }

    [Fact]
    public void MapToResponses_Page_ShouldProjectItemsAndPreservePaging()
    {
        // Arrange:
        var page = new PageModel<TestEntity>(
            Items: new[] { Entity("first"), Entity("second") },
            Page: 2,
            PageSize: 10,
            TotalCount: 25);

        // Act:
        var responses = page.MapToResponses<TestEntity, TestParameters, TestResponse>(
            (source, target) =>
            {
                target.Name = source.Name;
                target.Label = "paged";
            });

        // Assert:
        responses.Items.Should().HaveCount(2);
        responses.Items.Select(response => response.Name)
            .Should().ContainInOrder("first", "second");
        responses.Items.Should().OnlyContain(response => response.Label == "paged");
        responses.Page.Should().Be(2);
        responses.PageSize.Should().Be(10);
        responses.TotalCount.Should().Be(25);
        responses.TotalPages.Should().Be(3);
    }

    [Fact]
    public void MapToResponses_EmptyPage_ShouldPreservePagingMetadata()
    {
        // Arrange:
        var page = new PageModel<TestEntity>(
            Items: Array.Empty<TestEntity>(),
            Page: 1,
            PageSize: 20,
            TotalCount: 0);

        // Act:
        var responses = page.MapToResponses<TestEntity, TestParameters, TestResponse>(
            (_, _) => { });

        // Assert:
        responses.Items.Should().BeEmpty();
        responses.Page.Should().Be(1);
        responses.PageSize.Should().Be(20);
        responses.TotalCount.Should().Be(0);
        responses.TotalPages.Should().Be(0);
    }

    #endregion
}