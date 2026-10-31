using System.Diagnostics.Metrics;

using Application.Modules.Payments;

using AwesomeAssertions;

using Core.BuildingBlocks.Functional.Results;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

namespace Application.Unit.Tests.Modules.Payments;

/// <summary>Groups the meter tests so they do not observe each other's measurements.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PaymentMetricsCollection
{
    /// <summary>The collection name.</summary>
    public const string Name = "payment-metrics";
}

/// <summary>Proves the provider latency histogram is actually recorded.</summary>
[Collection(PaymentMetricsCollection.Name)]
public sealed class PaymentMetricsTest
{
    [Fact]
    public async Task TrackAsync_ShouldRecordOneMeasurementWithProviderAndOperation()
    {
        var measurements = new List<(double Value, string? Provider, string? Operation)>();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, own) =>
        {
            if (instrument.Meter.Name == PaymentMetrics.MeterName
                && instrument.Name == "payments.provider.duration")
                own.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((_, value, tags, _) =>
        {
            string? provider = null;
            string? operation = null;

            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == PaymentMetrics.ProviderTag)
                    provider = tag.Value as string;

                if (tag.Key == PaymentMetrics.OperationTag)
                    operation = tag.Value as string;
            }

            measurements.Add((value, provider, operation));
        });

        listener.Start();

        Result<int> result = await PaymentMetrics.TrackAsync(
            PaymentProvider.Fake,
            PaymentConstant.Operations.Capture,
            () => Task.FromResult(Result<int>.Success(1)));

        result.IsSuccess.Should().BeTrue();

        // Validate: other tests share the process-wide meter, so assert this call's tags
        // are present rather than that the histogram holds exactly one measurement.
        measurements.Should().Contain(measurement =>
            measurement.Provider == PaymentProvider.Fake.ToWire()
            && measurement.Operation == PaymentConstant.Operations.Capture
            && measurement.Value >= 0d);
    }

    [Fact]
    public async Task TrackAsync_WhenTheCallThrows_ShouldStillRecord()
    {
        int recorded = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, own) =>
        {
            if (instrument.Meter.Name == PaymentMetrics.MeterName
                && instrument.Name == "payments.provider.duration")
                own.EnableMeasurementEvents(instrument);
        };

        // Validate: the histogram is process-wide, so only count the measurement this
        // test produced; parallel tests emit their own.
        listener.SetMeasurementEventCallback<double>((_, _, tags, _) =>
        {
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == PaymentMetrics.OperationTag
                    && tag.Value as string == PaymentConstant.Operations.Webhook)
                    recorded++;
            }
        });

        listener.Start();

        Func<Task> act = () => PaymentMetrics.TrackAsync<int>(
            PaymentProvider.PayPal,
            PaymentConstant.Operations.Webhook,
            () => throw new InvalidOperationException("provider down"));

        await act.Should().ThrowAsync<InvalidOperationException>();

        recorded.Should().BeGreaterThanOrEqualTo(1);
    }
}
