using Application.Features.Todos.Items.Models;
using Application.Features.Todos.Items.Validators;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Validators;

/// <summary>Verifies the read model composes the aggregate's monadic rules.</summary>
[Trait("Category", "Unit")]
public sealed class TodoReadModelValidationTest
{
    [Fact]
    public void Validate_WithValidRow_ShouldPass()
    {
        TodoReadModel model = NewModel();

        Result<TodoReadModel> result = TodoReadModelValidation.Validate(model);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithBlankTitle_ShouldFail()
    {
        TodoReadModel model = NewModel() with { Title = " " };

        Result<TodoReadModel> result = TodoReadModelValidation.Validate(model);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoOutcome.Failure.TitleRequired.Code);
    }

    [Fact]
    public void SharedGuard_ShouldWorkOnTheReadModel()
    {
        TodoReadModel model = NewModel();

        Result<TodoReadModel> result = Result<TodoReadModel>.Success(model)
            .EnsureTitle("Renamed");

        result.IsSuccess.Should().BeTrue();
    }

    private static TodoReadModel NewModel() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Title = "Read me",
        Status = TodoStatus.Pending,
        Priority = TodoPriority.Medium,
        CreatedAtUtc = new DateTimeOffset(2026, 10, 30, 9, 0, 0, TimeSpan.Zero),
        Version = 1,
    };
}
