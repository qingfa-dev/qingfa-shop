using Application.Modules.Payments.Intents.Commands;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;
using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;

using AwesomeAssertions;

using Core.BuildingBlocks.Functional.Results;
using Core.Modules.Payments;

using Infrastructure.Modules.Payments;
using Infrastructure.Modules.Payments.Reconciliation;
using Infrastructure.Payments.Configuration;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace Infrastructure.Unit.Tests.Modules.Payments;

/// <summary>Covers the reconciliation worker and its options.</summary>
public sealed class PaymentReconciliationTest
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RunOnce_WhenDisabled_ShouldNotQuery()
    {
        var store = new Mock<IPaymentReadStore>();

        Result<int> result = await Build(store: store, enabled: false).RunOnceAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
        store.Verify(
            x => x.ListForReconciliationAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunOnce_ShouldSettleACapturedDrift()
    {
        var store = new Mock<IPaymentReadStore>();
        store.Setup(x => x.ListForReconciliationAsync(Now.AddMinutes(-10), 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<PaymentReadModel>>.Success([Row("fake", "fake_1")]));

        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(x => x.Provider).Returns(PaymentProvider.Fake);
        gateway.Setup(x => x.RetrieveAsync("fake_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentIntentResult>.Success(new PaymentIntentResult(
                "fake_1", PaymentProviderIntentStatus.Captured, null, null, null, null)));

        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<SettlePayment.UseCase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentResponseModel>.Success(new PaymentResponseModel()));

        Result<int> result = await Build(store: store, gateway: gateway.Object, mediator: mediator.Object)
            .RunOnceAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        mediator.Verify(x => x.Send(
            It.Is<SettlePayment.UseCase>(useCase => useCase.Observed == PaymentProviderEventType.Captured
                && useCase.ProviderReference == "fake_1"
                && useCase.ProviderEventId.StartsWith("reconcile:", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(PaymentProviderIntentStatus.Processing)]
    [InlineData(PaymentProviderIntentStatus.RequiresAction)]
    public async Task RunOnce_WhenStillOpen_ShouldNotSettle(PaymentProviderIntentStatus status)
    {
        var store = new Mock<IPaymentReadStore>();
        store.Setup(x => x.ListForReconciliationAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<PaymentReadModel>>.Success([Row("fake", "fake_1")]));

        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(x => x.Provider).Returns(PaymentProvider.Fake);
        gateway.Setup(x => x.RetrieveAsync("fake_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentIntentResult>.Success(new PaymentIntentResult(
                "fake_1", status, null, null, null, null)));

        var mediator = new Mock<IMediator>();

        Result<int> result = await Build(store: store, gateway: gateway.Object, mediator: mediator.Object)
            .RunOnceAsync();

        result.Value.Should().Be(0);
        mediator.Verify(x => x.Send(It.IsAny<SettlePayment.UseCase>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunOnce_WhenTheProviderFails_ShouldSkipThatPayment()
    {
        var store = new Mock<IPaymentReadStore>();
        store.Setup(x => x.ListForReconciliationAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<PaymentReadModel>>.Success([Row("fake", "fake_1")]));

        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(x => x.Provider).Returns(PaymentProvider.Fake);
        gateway.Setup(x => x.RetrieveAsync("fake_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentIntentResult>.Fail(
                PaymentsOutcome.Failure.ProviderUnavailable(PaymentProvider.Fake)));

        var mediator = new Mock<IMediator>();

        Result<int> result = await Build(store: store, gateway: gateway.Object, mediator: mediator.Object)
            .RunOnceAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
        mediator.Verify(x => x.Send(It.IsAny<SettlePayment.UseCase>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunOnce_WhenTheStoreFails_ShouldSurfaceTheFailure()
    {
        var store = new Mock<IPaymentReadStore>();
        store.Setup(x => x.ListForReconciliationAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<PaymentReadModel>>.Fail(
                PaymentsOutcome.Failure.RetentionFailed("boom")));

        Result<int> result = await Build(store: store).RunOnceAsync();

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 10, 10)]
    [InlineData(15, 0, 10)]
    [InlineData(15, 10, 0)]
    [InlineData(15, 10, 501)]
    public void Validate_OutOfRange_ShouldFail(int interval, int age, int batch)
    {
        var options = new PaymentReconciliationOptions
        {
            IntervalMinutes = interval,
            MinimumAgeMinutes = age,
            BatchSize = batch
        };

        new PaymentReconciliationOptionsValidator().Validate(null, options).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_Defaults_ShouldSucceed()
        => new PaymentReconciliationOptionsValidator()
            .Validate(null, new PaymentReconciliationOptions())
            .Failed.Should().BeFalse();

    /// <summary>Builds the worker over the supplied collaborators.</summary>
    private static PaymentReconciliationWorker Build(
        Mock<IPaymentReadStore>? store = null,
        IPaymentGateway? gateway = null,
        IMediator? mediator = null,
        bool enabled = true)
    {
        var services = new ServiceCollection();
        services.AddSingleton((store ?? new Mock<IPaymentReadStore>()).Object);
        services.AddSingleton(mediator ?? new Mock<IMediator>().Object);

        var registry = new Mock<IPaymentGatewayRegistry>();
        registry.Setup(x => x.Resolve(It.IsAny<PaymentProvider>()))
            .Returns(Result<IPaymentGateway>.Success(gateway ?? new Mock<IPaymentGateway>().Object));

        services.AddSingleton(registry.Object);

        return new PaymentReconciliationWorker(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new PaymentReconciliationOptions
            {
                Enabled = enabled,
                IntervalMinutes = 15,
                MinimumAgeMinutes = 10,
                BatchSize = 50
            }),
            new FixedClock(Now),
            NullLogger<PaymentReconciliationWorker>.Instance);
    }

    /// <summary>Builds a stale pending row.</summary>
    private static PaymentReadModel Row(string provider, string reference) => new()
    {
        Id = Guid.NewGuid(),
        Provider = provider,
        ProviderReference = reference,
        Status = "pending",
        Amount = 10m,
        Currency = "USD",
        ExternalReference = "cart-1",
        IdempotencyKey = "key-1",
        IdempotencyFingerprint = "fp-1",
        CustomerId = Guid.NewGuid(),
        CreatedAt = Now.AddHours(-1).UtcDateTime,
        UpdatedAt = Now.AddHours(-1).UtcDateTime
    };

    /// <summary>A clock fixed at a known instant.</summary>
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }
}
