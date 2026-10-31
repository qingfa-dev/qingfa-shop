using Application.BuildingBlocks.Querying.Pagination;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Refunds.Models;

using Core.Modules.Payments;

namespace Application.Modules.Payments.Persistence;

/// <summary>Reads projected payments.</summary>
public interface IPaymentReadStore
{
    /// <summary>Finds a payment by id.</summary>
    /// <param name="id">The payment id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The row, or <c>null</c> when absent.</returns>
    Task<PaymentReadModel?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Finds a payment by provider reference.</summary>
    /// <param name="provider">The provider.</param>
    /// <param name="providerReference">The provider reference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The row, or <c>null</c> when absent.</returns>
    Task<PaymentReadModel?> FindByProviderReferenceAsync(
        PaymentProvider provider,
        string providerReference,
        CancellationToken cancellationToken);

    /// <summary>Finds a payment by customer and idempotency key.</summary>
    /// <param name="customerId">The owning customer.</param>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The row, or <c>null</c> when absent.</returns>
    Task<PaymentReadModel?> FindByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>Lists a customer's payments.</summary>
    /// <param name="customerId">The owning customer.</param>
    /// <param name="externalReference">An optional external reference filter.</param>
    /// <param name="skip">Rows to skip.</param>
    /// <param name="take">Rows to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching rows.</returns>
    Task<IReadOnlyList<PaymentReadModel>> ListAsync(
        Guid customerId,
        string? externalReference,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>Counts a customer's payments.</summary>
    /// <param name="customerId">The owning customer.</param>
    /// <param name="externalReference">An optional external reference filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total count.</returns>
    Task<int> CountAsync(Guid customerId, string? externalReference, CancellationToken cancellationToken);

    /// <summary>Searches all payments for the admin surface.</summary>
    /// <param name="filter">The search filters.</param>
    /// <param name="page">The page request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One page of rows.</returns>
    Task<QueryPage<PaymentReadModel>> SearchAsync(
        PaymentSearchFilter filter,
        PageRequest page,
        CancellationToken cancellationToken);

    /// <summary>Finds one refund belonging to a payment.</summary>
    /// <param name="paymentId">The payment id.</param>
    /// <param name="refundId">The refund id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The row, or <c>null</c> when absent.</returns>
    Task<RefundReadModel?> FindRefundAsync(Guid paymentId, Guid refundId, CancellationToken cancellationToken);
}
