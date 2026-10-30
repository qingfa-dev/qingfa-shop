using Application.Common.EventSourcing;
using Application.Common.Persistence.Repositories;
using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Features.Todos.Reminders.Commands;
using Application.Features.Todos.Reminders.Mappings;
using Application.Features.Todos.Reminders.Models;
using Application.Features.Todos.Reminders.Queries;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Reminders;

namespace Application.Unit.Tests.Features.Todos.Reminders;

/// <summary>Unit tests for the reminder use cases.</summary>
[Trait("Category", "Unit")]
public sealed class ReminderUseCasesTest
{
    private static readonly DateTimeOffset Now = TodoHandlerFixture.Now;

    #region Schedule

    [Fact(DisplayName = "Schedule should persist a reminder owned by the ambient tenant")]
    public async Task Schedule_ShouldPersistAndMap()
    {
        var fixture = new TodoHandlerFixture();
        var repository = new Mock<IEventSourcedRepository<TodoReminder, Guid>>();
        repository
            .Setup(store => store.SaveAsync(It.IsAny<TodoReminder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var handler = new ScheduleTodoReminder.UseCaseHandler(
            repository.Object,
            fixture.Tenant,
            fixture.Clock);

        var command = new ScheduleTodoReminder.Request
        {
            TodoId = Guid.NewGuid(),
            RemindAtUtc = Now.AddHours(1),
        };

        Result<ScheduleTodoReminder.Response> result = await handler.Handle(
            new ScheduleTodoReminder.UseCase(command),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Value.TodoId.Should().Be(command.TodoId);
        result.Value.TenantId.Should().Be(TodoHandlerFixture.TenantId);
        result.Value.Status.Should().Be(ReminderStatus.Scheduled);
        repository.Verify(
            store => store.SaveAsync(
                It.Is<TodoReminder>(reminder => reminder.TodoId == command.TodoId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Schedule should reject a reminder in the past")]
    public async Task Schedule_WithPastInstant_ShouldFail()
    {
        var fixture = new TodoHandlerFixture();
        var handler = new ScheduleTodoReminder.UseCaseHandler(
            new Mock<IEventSourcedRepository<TodoReminder, Guid>>().Object,
            fixture.Tenant,
            fixture.Clock);

        var command = new ScheduleTodoReminder.Request
        {
            TodoId = Guid.NewGuid(),
            RemindAtUtc = Now.AddMinutes(-1),
        };

        Result<ScheduleTodoReminder.Response> result = await handler.Handle(
            new ScheduleTodoReminder.UseCase(command),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoReminderOutcome.Codes.RemindAtInPast);
    }

    [Fact(DisplayName = "Schedule validator should reject a missing todo id and instant")]
    public void Schedule_WithEmptyRequest_ShouldFailValidation()
    {
        var validation = new ScheduleTodoReminder.UseCaseValidator().Validate(
            new ScheduleTodoReminder.UseCase(new ScheduleTodoReminder.Request()));

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(error => error.PropertyName == "Request.TodoId");
        validation.Errors.Should().Contain(error => error.PropertyName == "Request.RemindAtUtc");
    }

    #endregion

    #region Transitions

    [Fact(DisplayName = "Snooze should persist the moved reminder")]
    public async Task Snooze_ShouldPersistAndMap()
    {
        var fixture = new TodoHandlerFixture();
        TodoReminder reminder = CreateReminder(fixture);
        var repository = new Mock<IEventSourcedRepository<TodoReminder, Guid>>();
        repository
            .Setup(store => store.LoadAsync(reminder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TodoReminder>.Success(reminder));
        repository
            .Setup(store => store.SaveAsync(It.IsAny<TodoReminder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var handler = new SnoozeTodoReminder.UseCaseHandler(
            repository.Object,
            fixture.Tenant,
            fixture.Clock);

        var command = new SnoozeTodoReminder.Request
        {
            TodoId = reminder.TodoId,
            ReminderId = reminder.Id,
            RemindAtUtc = Now.AddHours(2),
        };

        Result<SnoozeTodoReminder.Response> result = await handler.Handle(
            new SnoozeTodoReminder.UseCase(command),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ReminderStatus.Snoozed);
        result.Value.SnoozeCount.Should().Be(1);
    }

    [Fact(DisplayName = "Snooze should hide a reminder owned by another tenant")]
    public async Task Snooze_WithForeignTenant_ShouldReturnNotFound()
    {
        var fixture = new TodoHandlerFixture();
        TodoReminder reminder = TodoReminder.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddHours(1),
            Now).Value;
        var repository = new Mock<IEventSourcedRepository<TodoReminder, Guid>>();
        repository
            .Setup(store => store.LoadAsync(reminder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TodoReminder>.Success(reminder));

        var handler = new SnoozeTodoReminder.UseCaseHandler(
            repository.Object,
            fixture.Tenant,
            fixture.Clock);

        Result<SnoozeTodoReminder.Response> result = await handler.Handle(
            new SnoozeTodoReminder.UseCase(new SnoozeTodoReminder.Request
            {
                TodoId = reminder.TodoId,
                ReminderId = reminder.Id,
                RemindAtUtc = Now.AddHours(2),
            }),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoReminderOutcome.Codes.NotFound);
    }

    [Fact(DisplayName = "Dismiss should make the reminder terminal")]
    public async Task Dismiss_ShouldPersistTerminalState()
    {
        var fixture = new TodoHandlerFixture();
        TodoReminder reminder = CreateReminder(fixture);
        var repository = new Mock<IEventSourcedRepository<TodoReminder, Guid>>();
        repository
            .Setup(store => store.LoadAsync(reminder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TodoReminder>.Success(reminder));
        repository
            .Setup(store => store.SaveAsync(It.IsAny<TodoReminder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var handler = new DismissTodoReminder.UseCaseHandler(
            repository.Object,
            fixture.Tenant,
            fixture.Clock);

        Result result = await handler.Handle(
            new DismissTodoReminder.UseCase(new DismissTodoReminder.Request(reminder.Id, reminder.TodoId)),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(204);
        reminder.Status.Should().Be(ReminderStatus.Dismissed);
        reminder.DismissedAtUtc.Should().Be(Now);
    }

    #endregion

    #region Queries

    [Fact(DisplayName = "Get should map the projection row for the ambient tenant")]
    public async Task Get_WhenOwned_ShouldReturnModel()
    {
        var fixture = new TodoHandlerFixture();
        TodoReminderModel model = NewModel();
        var repository = new Mock<IReadableRepository<TodoReminderModel, Guid>>();
        repository
            .Setup(store => store.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TodoReminderModel>.Success(model));

        var handler = new GetTodoReminder.UseCaseHandler(repository.Object, fixture.Tenant);
        var query = new GetTodoReminder.UseCase(new GetTodoReminder.Request(Guid.NewGuid(), model.TodoId));

        Result<GetTodoReminder.Response> result = await handler.Handle(
            query,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.TodoId.Should().Be(model.TodoId);
    }

    [Fact(DisplayName = "Get should hide a projection row owned by another tenant")]
    public async Task Get_WithForeignTenant_ShouldReturnNotFound()
    {
        var fixture = new TodoHandlerFixture();
        TodoReminderModel model = NewModel() with { TenantId = Guid.NewGuid() };
        var repository = new Mock<IReadableRepository<TodoReminderModel, Guid>>();
        repository
            .Setup(store => store.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TodoReminderModel>.Success(model));

        var handler = new GetTodoReminder.UseCaseHandler(repository.Object, fixture.Tenant);

        Result<GetTodoReminder.Response> result = await handler.Handle(
            new GetTodoReminder.UseCase(new GetTodoReminder.Request(Guid.NewGuid(), model.TodoId)),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoReminderOutcome.Codes.NotFound);
    }

    [Fact(DisplayName = "Search should scope the mapped page to the ambient tenant")]
    public async Task Search_ShouldScopeTenant()
    {
        var fixture = new TodoHandlerFixture();
        fixture.RegisterReminderMap();
        var repository = new Mock<IMappableRepository<TodoReminderModel, Guid>>();
        QueryPage<SearchTodoReminders.Response> page = new(
            [new SearchTodoReminders.Response { Id = Guid.NewGuid() }],
            Page: 1,
            PageSize: 20,
            TotalCount: 1);
        repository
            .Setup(store => store.MapAsync(
                It.IsAny<Query<TodoReminderModel>>(),
                It.IsAny<Func<TodoReminderModel, SearchTodoReminders.Response>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QueryPage<SearchTodoReminders.Response>>.Success(page));

        var handler = new SearchTodoReminders.UseCaseHandler(
            repository.Object,
            fixture.Registry,
            fixture.Tenant);

        Result<QueryPage<SearchTodoReminders.Response>> result = await handler.Handle(
            new SearchTodoReminders.UseCase(new SearchTodoReminders.Request(new QueryParameters())),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        repository.Verify(
            store => store.MapAsync(
                It.Is<Query<TodoReminderModel>>(query => query.Filter != null),
                It.IsAny<Func<TodoReminderModel, SearchTodoReminders.Response>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Mapping

    [Fact(DisplayName = "Mapper should copy every reminder field")]
    public void Map_ShouldCopyFields()
    {
        var fixture = new TodoHandlerFixture();
        TodoReminder reminder = CreateReminder(fixture);

        ScheduleTodoReminder.Response response = reminder.ToModel<ScheduleTodoReminder.Response>();

        response.Id.Should().Be(reminder.Id);
        response.TodoId.Should().Be(reminder.TodoId);
        response.TenantId.Should().Be(reminder.TenantId);
        response.RemindAtUtc.Should().Be(reminder.RemindAtUtc);
        response.Status.Should().Be(reminder.Status);
        response.Version.Should().Be(reminder.Version);
    }

    #endregion

    #region Fixtures

    private static TodoReminder CreateReminder(TodoHandlerFixture fixture)
        => TodoReminder.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TodoHandlerFixture.TenantId,
            Now.AddHours(1),
            Now).Value;

    private static TodoReminderModel NewModel() => new()
    {
        Id = Guid.NewGuid(),
        TodoId = Guid.NewGuid(),
        TenantId = TodoHandlerFixture.TenantId,
        RemindAtUtc = Now.AddHours(1),
        Status = ReminderStatus.Scheduled,
        SnoozeCount = 0,
        Version = 1,
    };

    #endregion
}
