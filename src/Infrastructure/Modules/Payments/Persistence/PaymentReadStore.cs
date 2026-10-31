using Application.BuildingBlocks.Querying.Pagination;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;
using Application.Modules.Payments.Refunds.Models;

using Core.Modules.Payments;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>Reads projected payments from the database.</summary>
public sealed class PaymentReadStore(DbContext context) : IPaymentReadStore
{
    /// <inheritdoc />
    public Task<PaymentReadModel?> FindAsync(Guid id, CancellationToken cancellationToken)
        => context.Set<PaymentReadModel>().AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<PaymentReadModel?> FindByProviderReferenceAsync(
        PaymentProvider provider,
        string providerReference,
        CancellationToken cancellationToken)
    {
        string name = provider.ToWire();

        return context.Set<PaymentReadModel>().AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.Provider == name && row.ProviderReference == providerReference,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<PaymentReadModel?> FindByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken)
        => context.Set<PaymentReadModel>().AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.CustomerId == customerId && row.IdempotencyKey == idempotencyKey,
                cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentReadModel>> ListAsync(
        Guid customerId,
        string? externalReference,
        int skip,
        int take,
        CancellationToken cancellationToken)
        => await Filter(customerId, externalReference)
            .OrderByDescending(row => row.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> CountAsync(Guid customerId, string? externalReference, CancellationToken cancellationToken)
        => Filter(customerId, externalReference).CountAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<QueryPage<PaymentReadModel>> SearchAsync(
        PaymentSearchFilter filter,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PaymentReadModel> query = context.Set<PaymentReadModel>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(row => row.Status == filter.Status.ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(filter.Provider))
            query = query.Where(row => row.Provider == filter.Provider.ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(filter.Currency))
            query = query.Where(row => row.Currency == filter.Currency.ToUpperInvariant());

        if (filter.CreatedFrom is { } from)
            query = query.Where(row => row.CreatedAt >= from.UtcDateTime);

        if (filter.CreatedTo is { } to)
            query = query.Where(row => row.CreatedAt <= to.UtcDateTime);

        int total = await query.CountAsync(cancellationToken);

        List<PaymentReadModel> rows = await query
            .OrderByDescending(row => row.CreatedAt)
            .Skip((page.Page - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new QueryPage<PaymentReadModel>(rows, page.Page, page.PageSize, total);
    }

    /// <inheritdoc />
    public Task<RefundReadModel?> FindRefundAsync(
        Guid paymentId,
        Guid refundId,
        CancellationToken cancellationToken)
        => context.Set<RefundReadModel>().AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.PaymentId == paymentId && row.Id == refundId,
                cancellationToken);

    /// <summary>Builds the customer-scoped query.</summary>
    private IQueryable<PaymentReadModel> Filter(Guid customerId, string? externalReference)
    {
        IQueryable<PaymentReadModel> query = context.Set<PaymentReadModel>().AsNoTracking()
            .Where(row => row.CustomerId == customerId);

        return string.IsNullOrWhiteSpace(externalReference)
            ? query
            : query.Where(row => row.ExternalReference == externalReference);
    }
}
