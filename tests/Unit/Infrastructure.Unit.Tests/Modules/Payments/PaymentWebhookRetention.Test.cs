using Application.Modules.Payments.Persistence;

using Core.BuildingBlocks.Functional.Results;

using AwesomeAssertions;

using Infrastructure.Modules.Payments;
using Infrastructure.Modules.Payments.Retention;
using Core.Modules.Payments;

using Infrastructure.Payments.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace Infrastructure.Unit.Tests.Modules.Payments;

/// <summary>Covers the webhook retention worker and its options.</summary>
public sealed class PaymentWebhookRetentionTest
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RunOnce_ShouldPruneWithTheConfiguredWindow()
    {
        var store = new Mock<IWebhookEventStore>();
        DateTimeOffset? cutoffSeen = null;

        store.Setup(x => x.DeleteProcessedBeforeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback<DateTimeOffset, CancellationToken>((cutoff, _) => cutoffSeen = cutoff)
            .ReturnsAsync(Result<int>.Success(7));

        PaymentWebhookRetentionWorker worker = Build(store.Object, days: 30);

        Result<int> sweep = await worker.RunOnceAsync();

        sweep.IsSuccess.Should().BeTrue();
        sweep.Value.Should().Be(7);
        cutoffSeen.Should().Be(Now.AddDays(-30));
    }

    [Fact]
    public async Task RunOnce_WhenDisabled_ShouldNotTouchTheStore()
    {
        var store = new Mock<IWebhookEventStore>();

        PaymentWebhookRetentionWorker worker = Build(store.Object, enabled: false);

        Result<int> sweep = await worker.RunOnceAsync();

        sweep.IsSuccess.Should().BeTrue();
        sweep.Value.Should().Be(0);
        store.Verify(
            x => x.DeleteProcessedBeforeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunOnce_WhenTheStoreFails_ShouldReportFailureWithoutThrowing()
    {
        var store = new Mock<IWebhookEventStore>();
        store.Setup(x => x.DeleteProcessedBeforeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Fail(PaymentsOutcomeFailure()));

        PaymentWebhookRetentionWorker worker = Build(store.Object);

        Result<int> sweep = await worker.RunOnceAsync();

        sweep.IsFailure.Should().BeTrue();
        sweep.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.RetentionFailed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void Validate_RetentionOutOfRange_ShouldFail(int days)
    {
        var options = new PaymentRetentionOptions { WebhookEventRetentionDays = days };

        var result = new PaymentRetentionOptionsValidator().Validate(null, options);

        result.Failed.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    public void Validate_SweepIntervalOutOfRange_ShouldFail(int hours)
    {
        var options = new PaymentRetentionOptions { SweepIntervalHours = hours };

        var result = new PaymentRetentionOptionsValidator().Validate(null, options);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_Defaults_ShouldSucceed()
    {
        var result = new PaymentRetentionOptionsValidator().Validate(null, new PaymentRetentionOptions());

        result.Failed.Should().BeFalse();
    }

    [Fact]
    public void Validate_NullOptions_ShouldThrow()
    {
        Action act = () => new PaymentRetentionOptionsValidator().Validate(null, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>Builds a worker over the supplied store.</summary>
    private static PaymentWebhookRetentionWorker Build(
        IWebhookEventStore store,
        int days = 30,
        bool enabled = true)
    {
        var services = new ServiceCollection();
        services.AddSingleton(store);

        var options = Options.Create(new PaymentRetentionOptions
        {
            Enabled = enabled,
            WebhookEventRetentionDays = days
        });

        return new PaymentWebhookRetentionWorker(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            options,
            new FixedClock(Now),
            NullLogger<PaymentWebhookRetentionWorker>.Instance);
    }

    /// <summary>A failure used to simulate a store error.</summary>
    private static Core.BuildingBlocks.Functional.Errors.Error PaymentsOutcomeFailure()
        => Core.BuildingBlocks.Functional.Errors.Error.InternalServerError(
            "Payments.Webhook.RetentionFailed",
            "boom");

    /// <summary>A clock fixed at a known instant.</summary>
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }
}
