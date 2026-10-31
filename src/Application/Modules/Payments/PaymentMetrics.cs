using System.Diagnostics;
using System.Diagnostics.Metrics;

using Core.Modules.Payments;

namespace Application.Modules.Payments;

/// <summary>Payments counters and latency. Tags never carry customer or amount data.</summary>
public static class PaymentMetrics
{
    /// <summary>The meter name registered with OpenTelemetry.</summary>
    public const string MeterName = PaymentMetricConstant.MeterName;

    /// <summary>The provider tag name.</summary>
    public const string ProviderTag = PaymentMetricConstant.ProviderTag;

    /// <summary>The operation tag name.</summary>
    public const string OperationTag = PaymentMetricConstant.OperationTag;

    private static readonly Meter Meter = new(MeterName);

    /// <summary>Payments created.</summary>
    public static readonly Counter<long> Initiated =
        Meter.CreateCounter<long>(PaymentMetricConstant.Instrument.Initiated);

    /// <summary>Provider settlements applied.</summary>
    public static readonly Counter<long> Settled =
        Meter.CreateCounter<long>(PaymentMetricConstant.Instrument.Settled);

    /// <summary>Payments that failed.</summary>
    public static readonly Counter<long> Failed =
        Meter.CreateCounter<long>(PaymentMetricConstant.Instrument.Failed);

    /// <summary>Refunds confirmed by the provider.</summary>
    public static readonly Counter<long> RefundsSucceeded =
        Meter.CreateCounter<long>(PaymentMetricConstant.Instrument.RefundsSucceeded);

    /// <summary>Webhook deliveries accepted.</summary>
    public static readonly Counter<long> WebhooksReceived =
        Meter.CreateCounter<long>(PaymentMetricConstant.Instrument.WebhooksReceived);

    /// <summary>Webhook deliveries deduplicated.</summary>
    public static readonly Counter<long> WebhooksDuplicate =
        Meter.CreateCounter<long>(PaymentMetricConstant.Instrument.WebhooksDuplicate);

    /// <summary>Webhook deliveries rejected for amount mismatch.</summary>
    public static readonly Counter<long> WebhooksMismatch =
        Meter.CreateCounter<long>(PaymentMetricConstant.Instrument.WebhooksMismatch);

    /// <summary>Webhook deliveries pruned by the retention sweep.</summary>
    public static readonly Counter<long> WebhooksPruned =
        Meter.CreateCounter<long>(PaymentMetricConstant.Instrument.WebhooksPruned);

    /// <summary>Provider call latency in milliseconds.</summary>
    public static readonly Histogram<double> ProviderDuration =
        Meter.CreateHistogram<double>(
            PaymentMetricConstant.Instrument.ProviderDuration,
            PaymentMetricConstant.MillisecondsUnit);

    /// <summary>Records the latency of one provider call.</summary>
    /// <typeparam name="T">The call's result type.</typeparam>
    /// <param name="provider">The provider being called.</param>
    /// <param name="operation">The canonical operation name.</param>
    /// <param name="call">The provider call.</param>
    /// <returns>The call's result.</returns>
    public static async Task<T> TrackAsync<T>(
        PaymentProvider provider,
        string operation,
        Func<Task<T>> call)
    {
        ArgumentNullException.ThrowIfNull(call);

        long start = Stopwatch.GetTimestamp();

        try
        {
            return await call();
        }
        finally
        {
            ProviderDuration.Record(
                Stopwatch.GetElapsedTime(start).TotalMilliseconds,
                new KeyValuePair<string, object?>(ProviderTag, provider.ToWire()),
                new KeyValuePair<string, object?>(OperationTag, operation));
        }
    }
}
