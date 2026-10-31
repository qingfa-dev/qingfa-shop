using Application.Modules.Payments.Persistence;
using Application.Modules.Payments.Refunds.Models;

using Core.Modules.Payments;

using Application.BuildingBlocks.UseCases.Mediation.Queries;

using FluentValidation;

namespace Application.Modules.Payments.Refunds.Queries;

/// <summary>Reads one refund of a payment.</summary>
public static class GetRefund
{
    #region UseCase

    /// <summary>The query.</summary>
    /// <param name="PaymentId">The payment id.</param>
    /// <param name="RefundId">The refund id.</param>
    public sealed record UseCase(Guid PaymentId, Guid RefundId) : IQuery<RefundReadModel>;

    #endregion

    #region Validator

    /// <summary>Validates the query.</summary>
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        /// <summary>Creates the validator.</summary>
        public UseCaseValidator()
        {
            RuleFor(query => query.PaymentId).NotEmpty();
            RuleFor(query => query.RefundId).NotEmpty();
        }
    }

    #endregion

    #region Handler

    /// <summary>Handles the query.</summary>
    public sealed class UseCaseHandler(IPaymentReadStore store) : IQueryHandler<UseCase, RefundReadModel>
    {
        /// <inheritdoc />
        public async Task<Result<RefundReadModel>> Handle(UseCase query, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            RefundReadModel? row = await store.FindRefundAsync(
                query.PaymentId, query.RefundId, cancellationToken);

            return row is null
                ? Result<RefundReadModel>.Fail(PaymentsOutcome.Failure.RefundNotFound(query.RefundId))
                : Result<RefundReadModel>.Success(row);
        }
    }

    #endregion
}
