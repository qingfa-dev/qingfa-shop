using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Security.Authorization.Attributes;
using Application.Modules.Payments.Admin.Queries;
using Application.Modules.Payments.Intents.Commands;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Intents.Queries;
using Application.Modules.Payments.Permissions;
using Application.Modules.Payments.Refunds.Commands;
using Application.Modules.Payments.Refunds.Queries;

using Carter;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.BuildingBlocks.Functional.Results;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Infrastructure.BuildingBlocks.Adapters.Serialization;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using Web.Api.Endpoints.Payments.Models;

namespace Web.Api.Endpoints.Payments;

/// <summary>Carter module for the permission-gated payment surface.</summary>
public sealed class PaymentAdminEndpoint : ICarterModule
{
    private const string BasePath = "/api/admin/payments";

    /// <inheritdoc />
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder group = app.MapGroup(BasePath)
            .WithTags("Payments Admin")
            .RequireAuthorization();

        group.MapGet("/", SearchAsync)
            .WithName("SearchPayments")
            .WithSummary("Search payments.")
            .HasPermission(PaymentPermissionCatalog.Payments.Read);

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetPaymentForAdmin")
            .WithSummary("Get one payment.")
            .HasPermission(PaymentPermissionCatalog.Payments.Read);

        group.MapPost("/{id:guid}/capture", CaptureAsync)
            .WithName("CapturePayment")
            .WithSummary("Capture an authorized payment.")
            .HasPermission(PaymentPermissionCatalog.Payments.Manage);

        group.MapPost("/{id:guid}/refunds", RefundAsync)
            .WithName("RefundPayment")
            .WithSummary("Refund captured funds.")
            .HasPermission(PaymentPermissionCatalog.Payments.Refund);

        group.MapGet("/{id:guid}/refunds/{refundId:guid}", GetRefundAsync)
            .WithName("GetRefund")
            .WithSummary("Get one refund.")
            .HasPermission(PaymentPermissionCatalog.Payments.Read);
    }

    /// <summary>Searches all payments.</summary>
    private static async Task<IResult> SearchAsync(
        [AsParameters] QueryParameters parameters,
        string? status,
        string? provider,
        DateTimeOffset? createdFrom,
        DateTimeOffset? createdTo,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new SearchPayments.UseCase(
                    new PaymentSearchFilter(status, provider, createdFrom, createdTo),
                    new PageRequest(
                        parameters.Page ?? 1,
                        PaymentConstant.Paging.ClampSize(parameters.PageSize))),
                cancellationToken))
            .ToApiResult();

    /// <summary>Reads one payment without an ownership filter.</summary>
    private static async Task<IResult> GetAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new GetPayment.UseCase(id, null), cancellationToken)).ToApiResult();

    /// <summary>Captures an authorized payment.</summary>
    private static async Task<IResult> CaptureAsync(
        Guid id,
        CapturePaymentBody? body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        Money? amount = null;

        if (body?.Amount is not null)
        {
            Result<Money> converted = body.Amount.ToMoney();

            if (converted.IsFailure)
                return Result.Fail(converted.Errors).ToApiResult();

            amount = converted.Value;
        }

        return (await mediator.Send(new CapturePayment.UseCase(id, amount), cancellationToken))
            .ToApiResult();
    }

    /// <summary>Refunds captured funds.</summary>
    private static async Task<IResult> RefundAsync(
        Guid id,
        RefundPaymentBody body,
        [FromHeader(Name = PaymentConstant.Headers.IdempotencyKey)] string? idempotencyKey,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        // Validate: the header is required by contract, and a 422 from the validator would
        // not match the documented 400.
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Result.Fail(PaymentsOutcome.Failure.IdempotencyKeyRequired()).ToApiResult();

        Result<Money> amount = body.Amount.ToMoney();

        if (amount.IsFailure)
            return Result.Fail(amount.Errors).ToApiResult();

        return (await mediator.Send(
                new RefundPayment.UseCase(
                    id,
                    new RefundPayment.Request(amount.Value, body.Reason),
                    idempotencyKey),
                cancellationToken))
            .ToApiResult(refund => $"{BasePath}/{id}/refunds/{refund.Id}");
    }

    /// <summary>Reads one refund.</summary>
    private static async Task<IResult> GetRefundAsync(
        Guid id,
        Guid refundId,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new GetRefund.UseCase(id, refundId), cancellationToken)).ToApiResult();
}
