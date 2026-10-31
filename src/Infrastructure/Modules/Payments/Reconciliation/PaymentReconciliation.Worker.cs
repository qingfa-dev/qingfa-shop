using Application.Modules.Payments;
using Application.Modules.Payments.Intents.Commands;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;
using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;

using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Infrastructure.Payments.Configuration;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Modules.Payments.Reconciliation;

/// <summary>Compares stale local payments against the provider and settles any drift.</summary>
/// <remarks>
/// Webhooks are the fast path; this sweep is the safety net for a delivery that never arrived. It only
/// reads provider state and settles through the same <see cref="SettlePayment"/> command the webhook and
/// confirm paths use, so it can never invent a transition the aggregate rejects.
/// </remarks>
public sealed class PaymentReconciliationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PaymentReconciliationOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<PaymentReconciliationWorker> _logger;

    /// <summary>Creates the reconciliation worker.</summary>
    /// <param name="scopeFactory">Scope factory for the scoped stores and mediator.</param>
    /// <param name="options">The reconciliation configuration.</param>
    /// <param name="clock">The clock used to compute the cutoff.</param>
    /// <param name="logger">The logger.</param>
    public PaymentReconciliationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<PaymentReconciliationOptions> options,
        TimeProvider clock,
        ILogger<PaymentReconciliationWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.IntervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                PaymentReconciliationLoggers.SweepFailed(_logger, exception.Message);
            }
        }
    }

    /// <summary>Runs one reconciliation sweep.</summary>
    /// <param name="cancellationToken">A token that cancels the sweep.</param>
    /// <returns>The number of payments settled, or the first failure.</returns>
    public async Task<Result<int>> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return Result<int>.Success(0);

        DateTimeOffset cutoff = _clock.GetUtcNow().AddMinutes(-_options.MinimumAgeMinutes);

        using IServiceScope scope = _scopeFactory.CreateScope();

        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        IPaymentGatewayRegistry registry = scope.ServiceProvider.GetRequiredService<IPaymentGatewayRegistry>();
        IPaymentReadStore store = scope.ServiceProvider.GetRequiredService<IPaymentReadStore>();

        Result<IReadOnlyList<PaymentReadModel>> candidates = await store
            .ListForReconciliationAsync(cutoff, _options.BatchSize, cancellationToken)
            .ConfigureAwait(false);

        if (candidates.IsFailure)
            return Result<int>.Fail(candidates.Errors);

        int settled = 0;

        foreach (PaymentReadModel candidate in candidates.Value)
        {
            bool reconciled = await ReconcileAsync(candidate, registry, mediator, cancellationToken)
                .ConfigureAwait(false);

            if (reconciled)
                settled++;
        }

        if (settled > 0)
        {
            PaymentMetrics.Reconciled.Add(settled);
            PaymentReconciliationLoggers.Reconciled(_logger, settled);
        }

        return Result<int>.Success(settled);
    }

    /// <summary>Asks the provider for one payment and settles any drift.</summary>
    private async Task<bool> ReconcileAsync(
        PaymentReadModel candidate,
        IPaymentGatewayRegistry registry,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        Result<IPaymentGateway> gateway = registry.Resolve(PaymentProviderExtension.FromWire(candidate.Provider));

        if (gateway.IsFailure || candidate.ProviderReference is null)
            return false;

        Result<PaymentIntentResult> intent = await PaymentMetrics.TrackAsync(
            gateway.Value.Provider,
            PaymentConstant.Operations.Retrieve,
            () => gateway.Value.RetrieveAsync(candidate.ProviderReference, cancellationToken))
            .ConfigureAwait(false);

        if (intent.IsFailure)
            return false;

        // Enforce: a payment the provider still considers open needs no settlement.
        if (intent.Value.Status is PaymentProviderIntentStatus.RequiresAction
            or PaymentProviderIntentStatus.Processing)
            return false;

        Result<PaymentResponseModel> settled = await mediator.Send(
            new SettlePayment.UseCase(
                gateway.Value.Provider,
                candidate.ProviderReference,
                intent.Value.Status.ToEventType(),
                null,
                intent.Value.FailureCode,
                $"{PaymentConstant.IdempotencyPrefixes.Reconcile}{candidate.Id:N}"),
            cancellationToken).ConfigureAwait(false);

        if (settled.IsFailure)
        {
            PaymentReconciliationLoggers.SettleFailed(
                _logger,
                candidate.Id,
                settled.Errors[0].Code);
        }

        return settled.IsSuccess;
    }
}

/// <summary>Source-generated log messages for the reconciliation worker.</summary>
internal static partial class PaymentReconciliationLoggers
{
    /// <summary>Logs a sweep that settled payments.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">Payments settled.</param>
    [LoggerMessage(
        EventId = PaymentReconciliationConstant.LogEventId.Reconciled,
        Level = LogLevel.Information,
        Message = "Reconciled {Count} payments against the provider.")]
    public static partial void Reconciled(ILogger logger, int count);

    /// <summary>Logs a failed settlement for one payment.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="paymentId">The payment id.</param>
    /// <param name="code">The failure code.</param>
    [LoggerMessage(
        EventId = PaymentReconciliationConstant.LogEventId.SettleFailed,
        Level = LogLevel.Warning,
        Message = "Reconciliation could not settle payment {PaymentId}: {Code}")]
    public static partial void SettleFailed(ILogger logger, Guid paymentId, string code);

    /// <summary>Logs a failed sweep.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reason">Why the sweep failed.</param>
    [LoggerMessage(
        EventId = PaymentReconciliationConstant.LogEventId.SweepFailed,
        Level = LogLevel.Warning,
        Message = "Payment reconciliation sweep failed: {Reason}")]
    public static partial void SweepFailed(ILogger logger, string reason);

}
