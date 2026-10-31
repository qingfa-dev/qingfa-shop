using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;

using Core.Modules.Payments;

using Application.BuildingBlocks.UseCases.Mediation.Queries;

using FluentValidation;

namespace Application.Modules.Payments.Intents.Queries;

/// <summary>Reads one payment; a null customer means an admin read.</summary>
public static class GetPayment
{
    #region UseCase

    /// <summary>The query.</summary>
    /// <param name="PaymentId">The payment id.</param>
    /// <param name="CustomerId">The owning customer, or null for an admin read.</param>
    public sealed record UseCase(Guid PaymentId, Guid? CustomerId) : IQuery<PaymentResponseModel>;

    #endregion

    #region Validator

    /// <summary>Validates the query.</summary>
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        /// <summary>Creates the validator.</summary>
        public UseCaseValidator() => RuleFor(query => query.PaymentId).NotEmpty();
    }

    #endregion

    #region Handler

    /// <summary>Handles the query.</summary>
    public sealed class UseCaseHandler(IPaymentReadStore store) : IQueryHandler<UseCase, PaymentResponseModel>
    {
        /// <inheritdoc />
        public async Task<Result<PaymentResponseModel>> Handle(UseCase query, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            PaymentReadModel? row = await store.FindAsync(query.PaymentId, cancellationToken);

            // Enforce: another customer's payment is reported as absent, never forbidden.
            bool visible = row is not null
                && (query.CustomerId is null || row.CustomerId == query.CustomerId);

            return visible
                ? Result<PaymentResponseModel>.Success(row!.ToResponse())
                : Result<PaymentResponseModel>.Fail(PaymentsOutcome.Failure.NotFound(query.PaymentId));
        }
    }

    #endregion
}
