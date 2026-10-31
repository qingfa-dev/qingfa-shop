using Application.Modules.Todos.Items.Mappings;
using Application.Modules.Todos.Items.Models;

using Core.Modules.Todos.Items;

namespace Application.Unit.Tests.Modules.Todos.Items.Mappings;

/// <summary>Specification for the todo read-model mapper overloads.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Application")]
[Trait("Feature", "Todos")]
public sealed class TodoReadModelMapperTest
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToModel_ShouldMapEveryResponseField()
    {
        // Arrange:
        Guid id = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid listId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        TodoReadModel model = new()
        {
            Id = id,
            TenantId = tenantId,
            Title = "Write report",
            Description = "Draft the quarterly report",
            Status = TodoStatus.Completed,
            Priority = TodoPriority.High,
            DueAtUtc = Now.AddDays(1),
            CompletedAtUtc = Now,
            ListId = listId,
            CategoryIds = [categoryId],
            CreatedAtUtc = Now.AddDays(-1),
            CreatedBy = "creator",
            ModifiedAtUtc = Now,
            ModifiedBy = "editor",
            IsDeleted = true,
            DeletedAtUtc = Now,
            DeletedBy = "deleter",
            Version = 7,
        };

        // Act:
        TodoResponseModel response = model.ToModel<TodoResponseModel>();

        // Assert:
        response.Id.Should().Be(id);
        response.TenantId.Should().Be(tenantId);
        response.Title.Should().Be("Write report");
        response.Description.Should().Be("Draft the quarterly report");
        response.Status.Should().Be(TodoStatus.Completed);
        response.Priority.Should().Be(TodoPriority.High);
        response.DueAtUtc.Should().Be(Now.AddDays(1));
        response.CompletedAtUtc.Should().Be(Now);
        response.ListId.Should().Be(listId);
        response.CategoryIds.Should().Equal(categoryId);
        response.IsCompleted.Should().BeTrue();
        response.CreatedAtUtc.Should().Be(Now.AddDays(-1));
        response.CreatedBy.Should().Be("creator");
        response.ModifiedAtUtc.Should().Be(Now);
        response.ModifiedBy.Should().Be("editor");
        response.IsDeleted.Should().BeTrue();
        response.DeletedAtUtc.Should().Be(Now);
        response.DeletedBy.Should().Be("deleter");
    }

    [Fact]
    public void ToSummary_ShouldMapEverySummaryField()
    {
        // Arrange:
        Guid id = Guid.NewGuid();
        Guid listId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        TodoReadModel model = new()
        {
            Id = id,
            Title = "Write report",
            Status = TodoStatus.InProgress,
            Priority = TodoPriority.Low,
            DueAtUtc = Now.AddDays(2),
            ListId = listId,
            CategoryIds = [categoryId],
            CreatedAtUtc = Now.AddDays(-1),
            Description = "Ignored by the summary",
            IsDeleted = true,
        };

        // Act:
        TodoSummaryResponseModel summary = model.ToSummary<TodoSummaryResponseModel>();

        // Assert:
        summary.Id.Should().Be(id);
        summary.Title.Should().Be("Write report");
        summary.Status.Should().Be(TodoStatus.InProgress);
        summary.Priority.Should().Be(TodoPriority.Low);
        summary.DueAtUtc.Should().Be(Now.AddDays(2));
        summary.ListId.Should().Be(listId);
        summary.CategoryIds.Should().Equal(categoryId);
        summary.IsCompleted.Should().BeFalse();
        summary.CreatedAtUtc.Should().Be(Now.AddDays(-1));
    }

    [Fact]
    public void ToModel_WithNullReadModel_ShouldThrow()
    {
        // Act:
        Action act = () => ((TodoReadModel)null!).ToModel<TodoResponseModel>();

        // Assert:
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToSummary_WithNullReadModel_ShouldThrow()
    {
        // Act:
        Action act = () => ((TodoReadModel)null!).ToSummary<TodoSummaryResponseModel>();

        // Assert:
        act.Should().Throw<ArgumentNullException>();
    }
}