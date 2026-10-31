using Application.BuildingBlocks.Querying.Pagination;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;
using Application.Modules.Payments.Refunds.Models;

using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>Reads projected payments from the database.</summary>
public sealed class PaymentReadStore(DbContext context) : IPaymentReadStore
{
    /// <inheritdoc />
    public async Task<Result<PaymentReadModel>> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        PaymentReadModel? row = await context.Set<PaymentReadModel>().AsNoTracking()
            .FirstOrDefaultAsync(model => model.Id == id, cancellationToken);

        return row is null
            ? Result<PaymentReadModel>.Fail(PaymentsOutcome.Failure.NotFound(id))
            : Result<PaymentReadModel>.Success(row);
    }

    /// <inheritdoc />
    public async Task<Result<PaymentReadModel>> FindByProviderReferenceAsync(
        PaymentProvider provider,
        string providerReference,
        CancellationToken cancellationToken)
    {
        string name = provider.ToWire();

        PaymentReadModel? row = await context.Set<PaymentReadModel>().AsNoTracking()
            .FirstOrDefaultAsync(
                model => model.Provider == name && model.ProviderReference == providerReference,
                cancellationToken);

        return row is null
            ? Result<PaymentReadModel>.Fail(PaymentsOutcome.Failure.ReferenceNotFound(providerReference))
            : Result<PaymentReadModel>.Success(row);
    }

    /// <inheritdoc />
    public async Task<Result<PaymentReadModel>> FindByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        PaymentReadModel? row = await context.Set<PaymentReadModel>().AsNoTracking()
            .FirstOrDefaultAsync(
                model => model.CustomerId == customerId && model.IdempotencyKey == idempotencyKey,
                cancellationToken);

        return row is null
            ? Result<PaymentReadModel>.Fail(PaymentsOutcome.Failure.NotFound(Guid.Empty))
            : Result<PaymentReadModel>.Success(row);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PaymentReadModel>>> ListAsync(
        Guid customerId,
        string? externalReference,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        List<PaymentReadModel> rows = await Filter(customerId, externalReference)
            .OrderByDescending(model => model.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PaymentReadModel>>.Success(rows);
    }

    /// <inheritdoc />
    public async Task<Result<int>> CountAsync(
        Guid customerId,
        string? externalReference,
        CancellationToken cancellationToken)
        => Result<int>.Success(
            await Filter(customerId, externalReference).CountAsync(cancellationToken));

    /// <inheritdoc />
    public async Task<Result<QueryPage<PaymentReadModel>>> SearchAsync(
        PaymentSearchFilter filter,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PaymentReadModel> query = context.Set<PaymentReadModel>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(model => model.Status == filter.Status.ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(filter.Provider))
            query = query.Where(model => model.Provider == filter.Provider.ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(filter.Currency))
            query = query.Where(model => model.Currency == filter.Currency.ToUpperInvariant());

        if (filter.CreatedFrom is { } from)
            query = query.Where(model => model.CreatedAt >= from.UtcDateTime);

        if (filter.CreatedTo is { } to)
            query = query.Where(model => model.CreatedAt <= to.UtcDateTime);

        int total = await query.CountAsync(cancellationToken);

        List<PaymentReadModel> rows = await query
            .OrderByDescending(model => model.CreatedAt)
            .Skip((page.Page - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return Result<QueryPage<PaymentReadModel>>.Success(
            new QueryPage<PaymentReadModel>(rows, page.Page, page.PageSize, total));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PaymentReadModel>>> ListForReconciliationAsync(
        DateTimeOffset receivedBefore,
        int take,
        CancellationToken cancellationToken)
    {
        string[] unsettled =
        [
            PaymentStatus.Pending.ToWire(),
            PaymentStatus.Processing.ToWire(),
            PaymentStatus.Authorized.ToWire()
        ];

        DateTime cutoff = receivedBefore.UtcDateTime;

        List<PaymentReadModel> rows = await context.Set<PaymentReadModel>().AsNoTracking()
            .Where(model => unsettled.Contains(model.Status) && model.CreatedAt < cutoff)
            .OrderBy(model => model.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PaymentReadModel>>.Success(rows);
    }

    /// <inheritdoc />
    public async Task<Result<RefundReadModel>> FindRefundAsync(
        Guid paymentId,
        Guid refundId,
        CancellationToken cancellationToken)
    {
        RefundReadModel? row = await context.Set<RefundReadModel>().AsNoTracking()
            .FirstOrDefaultAsync(
                model => model.PaymentId == paymentId && model.Id == refundId,
                cancellationToken);

        return row is null
            ? Result<RefundReadModel>.Fail(PaymentsOutcome.Failure.RefundNotFound(refundId))
            : Result<RefundReadModel>.Success(row);
    }

    /// <summary>Builds the customer-scoped query.</summary>
    private IQueryable<PaymentReadModel> Filter(Guid customerId, string? externalReference)
    {
        IQueryable<PaymentReadModel> query = context.Set<PaymentReadModel>().AsNoTracking()
            .Where(model => model.CustomerId == customerId);

        return string.IsNullOrWhiteSpace(externalReference)
            ? query
            : query.Where(model => model.ExternalReference == externalReference);
    }
}
