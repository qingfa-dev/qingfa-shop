using System.Text.Json;

using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Security.Authentication.Contexts;
using Application.Modules.Payments.Intents.Commands;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Intents.Queries;

using Carter;

using Core.BuildingBlocks.Functional.Results;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Infrastructure.BuildingBlocks.Adapters.Serialization;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using Web.Api.Endpoints.Payments.Models;

namespace Web.Api.Endpoints.Payments;

/// <summary>Carter module for the customer payment surface.</summary>
public sealed class PaymentEndpoint : ICarterModule
{
    private const string BasePath = "/api/payments";

    /// <inheritdoc />
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder group = app.MapGroup(BasePath)
            .WithTags("Payments")
            .RequireAuthorization();

        group.MapPost("/", CreateAsync)
            .WithName("CreatePayment")
            .WithSummary("Create a payment intent.");

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetPayment")
            .WithSummary("Get one payment owned by the caller.");

        group.MapGet("/", ListAsync)
            .WithName("ListMyPayments")
            .WithSummary("List the caller's payments.");

        group.MapPost("/{id:guid}/confirm", ConfirmAsync)
            .WithName("ConfirmPayment")
            .WithSummary("Refresh a payment after the hosted checkout returns.");

        group.MapPost("/{id:guid}/cancel", CancelAsync)
            .WithName("CancelPayment")
            .WithSummary("Void an uncaptured payment.");
    }

    /// <summary>Creates a payment intent.</summary>
    private static async Task<IResult> CreateAsync(
        CreatePaymentBody body,
        [FromHeader(Name = PaymentConstant.Headers.IdempotencyKey)] string? idempotencyKey,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(httpContext);

        // Validate: the header is required by contract, and a 422 from the validator would
        // not match the documented 400.
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Result.Fail(PaymentsOutcome.Failure.IdempotencyKeyRequired()).ToApiResult();

        Result<CreatePayment.Request> request = body.ToRequest();

        if (request.IsFailure)
            return Result.Fail(request.Errors).ToApiResult();

        Result<PaymentResponseModel> result = await mediator.Send(
            new CreatePayment.UseCase(request.Value, idempotencyKey),
            cancellationToken);

        // Handle: replay responses carry the stored resource and announce themselves.
        if (result.IsSuccess && result.Metadata.ContainsKey("IdempotentReplay"))
            httpContext.Response.Headers[PaymentConstant.Headers.IdempotentReplay] =
                PaymentConstant.Headers.ReplayTrue;

        return result.ToApiResult(payment => $"{BasePath}/{payment.Id}");
    }

    /// <summary>Reads one payment owned by the caller.</summary>
    private static async Task<IResult> GetAsync(
        Guid id,
        IUserContext userContext,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new GetPayment.UseCase(id, Customer(userContext)), cancellationToken))
            .ToApiResult();

    /// <summary>Lists the caller's payments.</summary>
    private static async Task<IResult> ListAsync(
        [AsParameters] QueryParameters parameters,
        string? externalReference,
        IUserContext userContext,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new ListMyPayments.UseCase(
                    Customer(userContext),
                    externalReference,
                    new PageRequest(
                        parameters.Page ?? 1,
                        parameters.PageSize ?? PaymentConstant.Defaults.DefaultPageSize)),
                cancellationToken))
            .ToApiResult();

    /// <summary>Refreshes a payment after the hosted checkout returns.</summary>
    private static async Task<IResult> ConfirmAsync(
        Guid id,
        IUserContext userContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userContext);

        // Validate: the caller must own the payment before it is refreshed.
        Result<PaymentResponseModel> owned = await mediator.Send(
            new GetPayment.UseCase(id, Customer(userContext)), cancellationToken);

        if (owned.IsFailure)
            return Result.Fail(owned.Errors).ToApiResult();

        return (await mediator.Send(new ConfirmPayment.UseCase(id), cancellationToken)).ToApiResult();
    }

    /// <summary>Voids an uncaptured payment.</summary>
    private static async Task<IResult> CancelAsync(
        Guid id,
        IUserContext userContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userContext);

        Result<PaymentResponseModel> owned = await mediator.Send(
            new GetPayment.UseCase(id, Customer(userContext)), cancellationToken);

        if (owned.IsFailure)
            return Result.Fail(owned.Errors).ToApiResult();

        return (await mediator.Send(new CancelPayment.UseCase(id), cancellationToken)).ToApiResult();
    }

    /// <summary>Reads the caller's customer id.</summary>
    private static Guid Customer(IUserContext userContext)
    {
        ArgumentNullException.ThrowIfNull(userContext);

        return Guid.TryParse(userContext.UserId, out Guid id)
            ? id
            : throw new InvalidOperationException("The access token has no usable subject.");
    }
}
