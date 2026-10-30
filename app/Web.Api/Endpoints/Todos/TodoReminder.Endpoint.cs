using Core.Common.Monad.Results;

using Application.Features.Todos.Reminders.Commands;
using Application.Features.Todos.Reminders.Queries;

using Carter;

using MediatR;

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

        group.MapPost("/", ScheduleAsync)
            .WithName("ScheduleTodoReminder");

        group.MapPatch("/{reminderId:guid}/snooze", SnoozeAsync)
            .WithName("SnoozeTodoReminder");

        group.MapDelete("/{reminderId:guid}", DismissAsync)
            .WithName("DismissTodoReminder");

        group.MapGet("/{reminderId:guid}", GetAsync)
            .WithName("GetTodoReminder");
    }

    private static async Task<IResult> ScheduleAsync(
        Guid todoId,
        ScheduleRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        Result<ScheduleTodoReminder.Response> result = await mediator.Send(
            new ScheduleTodoReminder.UseCase(new ScheduleTodoReminder.Request(todoId, request.RemindAtUtc)),
            cancellationToken);

        return ToResult(result, value => Results.Created(
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
        _ = todoId;
        Result<SnoozeTodoReminder.Response> result = await mediator.Send(
            new SnoozeTodoReminder.UseCase(new SnoozeTodoReminder.Request(reminderId, request.RemindAtUtc)),
            cancellationToken);

        return ToResult(result, Results.Ok);
    }

    private static async Task<IResult> DismissAsync(
        Guid todoId,
        Guid reminderId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        _ = todoId;
        Result<DismissTodoReminder.Response> result = await mediator.Send(
            new DismissTodoReminder.UseCase(new DismissTodoReminder.Request(reminderId)),
            cancellationToken);

        return ToResult(result, Results.Ok);
    }

    private static async Task<IResult> GetAsync(
        Guid todoId,
        Guid reminderId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        _ = todoId;
        Result<GetTodoReminder.Response> result = await mediator.Send(
            new GetTodoReminder.UseCase(new GetTodoReminder.Request(reminderId)),
            cancellationToken);

        return ToResult(result, Results.Ok);
    }

    private static IResult ToResult<T>(Result<T> result, Func<T, IResult> success)
        => result.IsSuccess
            ? success(result.Value)
            : Results.Problem(
                title: result.Errors[0].Code,
                detail: result.Errors[0].Message,
                statusCode: result.StatusCode);

    /// <summary>Schedule request body.</summary>
    public sealed record ScheduleRequest(DateTimeOffset RemindAtUtc);

    /// <summary>Snooze request body.</summary>
    public sealed record SnoozeRequest(DateTimeOffset RemindAtUtc);
}
