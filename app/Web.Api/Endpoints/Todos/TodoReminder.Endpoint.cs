using Application.Common.Querying;
using Application.Common.Security.Authorization.Attributes;
using Application.Common.Security.Authorization.Registry;
using Application.Features.Todos.Reminders.Commands;
using Application.Features.Todos.Reminders.Queries;

using Carter;

using Core.Common.Monad.Results;

using MediatR;

using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace Web.Api.Endpoints.Todos;

/// <summary>Carter module for todo reminders.</summary>
public sealed class TodoReminderEndpoint : ICarterModule
{
    private const string BasePath = "/api/todos/{todoId:guid}/reminders";

    /// <inheritdoc />
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BasePath)
            .WithTags("TodoReminders")
            .RequireAuthorization();

        group.MapGet("/", SearchAsync)
            .WithName("SearchTodoReminders")
            .WithSummary("List the reminders of one todo.")
            .HasPermission(PermissionContext.Todo.Items.Read);

        group.MapGet("/{reminderId:guid}", GetAsync)
            .WithName("GetTodoReminder")
            .WithSummary("Get one reminder of a todo.")
            .HasPermission(PermissionContext.Todo.Items.Read);

        group.MapPost("/", ScheduleAsync)
            .WithName("ScheduleTodoReminder")
            .WithSummary("Schedule a reminder for a todo.")
            .HasPermission(PermissionContext.Todo.Items.Create);

        group.MapPatch("/{reminderId:guid}/snooze", SnoozeAsync)
            .WithName("SnoozeTodoReminder")
            .WithSummary("Move a reminder forward.")
            .HasPermission(PermissionContext.Todo.Items.Update);

        group.MapDelete("/{reminderId:guid}", DismissAsync)
            .WithName("DismissTodoReminder")
            .WithSummary("Dismiss a reminder permanently.")
            .HasPermission(PermissionContext.Todo.Items.Update);
    }

    private static async Task<IResult> SearchAsync(
        Guid todoId,
        [AsParameters] QueryParameters parameters,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new SearchTodoReminders.UseCase(new SearchTodoReminders.Request(parameters, todoId)),
                cancellationToken))
            .ToHttpResult();

    private static async Task<IResult> GetAsync(
        Guid todoId,
        Guid reminderId,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new GetTodoReminder.UseCase(new GetTodoReminder.Request(reminderId, todoId)),
                cancellationToken))
            .ToHttpResult();

    private static async Task<IResult> ScheduleAsync(
        Guid todoId,
        ScheduleRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new ScheduleTodoReminder.Request
        {
            TodoId = todoId,
            RemindAtUtc = request.RemindAtUtc,
        };

        return (await mediator.Send(
                new ScheduleTodoReminder.UseCase(command),
                cancellationToken))
            .ToHttpResult(value => HttpResults.Created(
                $"/api/todos/{todoId}/reminders/{value.Id}",
                value));
    }

    private static async Task<IResult> SnoozeAsync(
        Guid todoId,
        Guid reminderId,
        SnoozeRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new SnoozeTodoReminder.Request
        {
            TodoId = todoId,
            ReminderId = reminderId,
            RemindAtUtc = request.RemindAtUtc,
        };

        return (await mediator.Send(
                new SnoozeTodoReminder.UseCase(command),
                cancellationToken))
            .ToHttpResult();
    }

    private static async Task<IResult> DismissAsync(
        Guid todoId,
        Guid reminderId,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new DismissTodoReminder.UseCase(new DismissTodoReminder.Request(reminderId, todoId)),
                cancellationToken))
            .ToHttpResult();

    /// <summary>Schedule request body.</summary>
    public sealed record ScheduleRequest(DateTimeOffset RemindAtUtc);

    /// <summary>Snooze request body.</summary>
    public sealed record SnoozeRequest(DateTimeOffset RemindAtUtc);
}
