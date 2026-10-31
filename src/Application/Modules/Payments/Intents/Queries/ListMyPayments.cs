using Application.BuildingBlocks.Querying.Pagination;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;

using Application.BuildingBlocks.UseCases.Mediation.Queries;

using FluentValidation;

namespace Application.Modules.Payments.Intents.Queries;

/// <summary>Lists a customer's payments.</summary>
public static class ListMyPayments
{
    #region UseCase

    /// <summary>The query.</summary>
    /// <param name="CustomerId">The owning customer.</param>
    /// <param name="ExternalReference">An optional external reference filter.</param>
    /// <param name="Page">The page request.</param>
    public sealed record UseCase(Guid CustomerId, string? ExternalReference, PageRequest Page)
        : IQuery<QueryPage<PaymentResponseModel>>;

    #endregion

    #region Validator

    /// <summary>Validates the query.</summary>
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        /// <summary>Creates the validator.</summary>
        public UseCaseValidator() => RuleFor(query => query.Page).NotNull();
    }

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

            int skip = (query.Page.Page - 1) * query.Page.PageSize;

            IReadOnlyList<PaymentReadModel> rows = await store.ListAsync(
                query.CustomerId, query.ExternalReference, skip, query.Page.PageSize, cancellationToken);

            int total = await store.CountAsync(query.CustomerId, query.ExternalReference, cancellationToken);

            return Result<QueryPage<PaymentResponseModel>>.Success(new QueryPage<PaymentResponseModel>(
                Items: [.. rows.Select(row => row.ToResponse())],
                Page: query.Page.Page,
                PageSize: query.Page.PageSize,
                TotalCount: total));
        }
    }

    #endregion
}
