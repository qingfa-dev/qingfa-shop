using Application.Common.Mediators.Queries;
using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Features.Todos.Reminders.Mappings;
using Application.Features.Todos.Common;
using Application.Features.Todos.Reminders.Models;

using FluentValidation;

namespace Application.Features.Todos.Reminders.Queries;

public static class SearchTodoReminders
{
    #region Request
    public sealed record Request(QueryParameters Parameters, Guid? TodoId = null);
    #endregion

    #region Response
    public sealed record Response : TodoReminderSummaryResponseModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request)
        : IQuery<QueryPage<Response>>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IMappableRepository<TodoReminderModel, Guid> repository,
        IQueryMapProvider mapProvider,
        ITenantContext tenantContext)
        : IQueryHandler<UseCase, QueryPage<Response>>
    {
        public async Task<Result<QueryPage<Response>>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(query.Request);

            Request request = query.Request;

            return await tenantContext.RequireTenant()
                .Bind(tenantId => Scope(
                        QueryBuilder<TodoReminderModel>.FromParameters(
                            mapProvider,
                            request.Parameters),
                        tenantId,
                        request.TodoId)
                    .DefaultOrderBy("remindAtUtc", Application.Common.Querying.Sorting.SortDirection.Ascending)
                    .BuildEntity(TodosOutcome.Failure.TabularQueryUnsupported))
                .BindAsync(
                    (parsed, ct) => repository.MapAsync(
                        parsed,
                        reminder => reminder.ToSummary<Response>(),
                        ct),
                    cancellationToken);
        }

        private static QueryBuilder<TodoReminderModel> Scope(
            QueryBuilder<TodoReminderModel> builder,
            Guid tenantId,
            Guid? todoId)
        {
            QueryBuilder<TodoReminderModel> scoped =
                builder.Where(model => model.TenantId == tenantId);

            // Filter: an optional todo id keeps the reminder list scoped to one todo.
            return todoId is Guid value
                ? scoped.Where(model => model.TodoId == value)
                : scoped;
        }
    }
    #endregion
}
