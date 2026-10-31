using Application.BuildingBlocks.Querying.Pagination;
using Application.Modules.Payments.Intents;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;

using Application.BuildingBlocks.UseCases.Mediation.Queries;

namespace Application.Modules.Payments.Admin.Queries;

/// <summary>Searches all payments for administrators.</summary>
public static class SearchPayments
{
    #region UseCase

    /// <summary>The query.</summary>
    /// <param name="Filter">The search filters.</param>
    /// <param name="Page">The page request.</param>
    public sealed record UseCase(PaymentSearchFilter Filter, PageRequest Page)
        : IQuery<QueryPage<PaymentResponseModel>>;

    #endregion


    #region Handler

    /// <summary>Handles the query.</summary>
    public sealed class UseCaseHandler(IPaymentReadStore store)
        : IQueryHandler<UseCase, QueryPage<PaymentResponseModel>>
    {
        /// <inheritdoc />
        public async Task<Result<QueryPage<PaymentResponseModel>>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            Result<QueryPage<PaymentReadModel>> page = await store.SearchAsync(
                query.Filter, query.Page, cancellationToken);

            return page.Map(found => new QueryPage<PaymentResponseModel>(
                Items: [.. found.Items.Select(row => row.ToResponse())],
                Page: found.Page,
                PageSize: found.PageSize,
                TotalCount: found.TotalCount));
        }
    }

    #endregion
}
