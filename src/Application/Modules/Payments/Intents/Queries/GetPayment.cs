using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;

using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

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
    public sealed class UseCaseHandler(
        IPaymentReadStore store,
        IEventSourcedRepository<Payment, Guid> repository) : IQueryHandler<UseCase, PaymentResponseModel>
    {
        /// <inheritdoc />
        public async Task<Result<PaymentResponseModel>> Handle(UseCase query, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            PaymentReadModel? row = await store.FindAsync(query.PaymentId, cancellationToken);

            // Enforce: another customer's payment is reported as absent, never forbidden.
            if (row is not null)
            {
                bool visible = query.CustomerId is null || row.CustomerId == query.CustomerId;

                return visible
                    ? Result<PaymentResponseModel>.Success(row.ToResponse())
                    : Result<PaymentResponseModel>.Fail(PaymentsOutcome.Failure.NotFound(query.PaymentId));
            }

            // Fallback: read the event stream while the projection is still catching up.
            Result<Payment> loaded = await repository.LoadAsync(query.PaymentId, cancellationToken);

            if (loaded.IsFailure)
                return Result<PaymentResponseModel>.Fail(PaymentsOutcome.Failure.NotFound(query.PaymentId));

            Payment payment = loaded.Value;
            bool owned = query.CustomerId is null || payment.CustomerId == query.CustomerId;

            return owned
                ? Result<PaymentResponseModel>.Success(payment.ToResponse())
                : Result<PaymentResponseModel>.Fail(PaymentsOutcome.Failure.NotFound(query.PaymentId));
        }
    }

    #endregion
}
