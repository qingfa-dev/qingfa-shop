using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Intents.Commands;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Intents.Queries;
using Application.Modules.Payments.Persistence;
using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;
using Application.Unit.Tests.Modules.Payments.Fixtures;

using AwesomeAssertions;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Moq;

namespace Application.Unit.Tests.Modules.Payments.Commands;

public sealed class PaymentPolicyEnforcementTest
{
    private static readonly Guid CustomerId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static Money Usd(decimal amount) => new(amount, new Currency("USD"));

    [Fact]
    public async Task Create_AboveTheConfiguredCeiling_ShouldFail()
    {
        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        var idempotency = new Mock<IPaymentIdempotencyStore>();
        var references = new Mock<IPaymentReferenceStore>();
        var registry = new Mock<IPaymentGatewayRegistry>();
        var user = new Mock<Application.BuildingBlocks.Security.Authentication.Contexts.IUserContext>();
        user.SetupGet(x => x.UserId).Returns(CustomerId.ToString());

        var handler = new CreatePayment.UseCaseHandler(
            repository.Object,
            idempotency.Object,
            references.Object,
            registry.Object,
            new StubPaymentPolicy
            {
                MaximumAmountsByCurrency = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                {
                    ["USD"] = 50m
                }
            },
            user.Object,
            TimeProvider.System);

        var request = new CreatePayment.Request(
            Usd(51m), PaymentProvider.Fake, "cart-big", PaymentCaptureMode.Automatic, null, null);

        var result = await handler.Handle(
            new CreatePayment.UseCase(request, "key-big"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.AmountExceedsLimit);
        registry.Verify(x => x.Resolve(It.IsAny<PaymentProvider>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithoutProviderExpiry_ShouldUseTheConfiguredLifetime()
    {
        var reservation = new IdempotencyReservation(Guid.NewGuid(), IsNew: true, FingerprintMatches: true);

        var idempotency = new Mock<IPaymentIdempotencyStore>();
        idempotency.Setup(x => x.ReserveAsync(CustomerId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IdempotencyReservation>.Success(reservation));

        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(x => x.Provider).Returns(PaymentProvider.Fake);
        gateway.Setup(x => x.CreateIntentAsync(It.IsAny<PaymentIntentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentIntentResult>.Success(new PaymentIntentResult(
                "fake_1", PaymentProviderIntentStatus.RequiresAction, null, null, null, null)));

        var registry = new Mock<IPaymentGatewayRegistry>();
        registry.Setup(x => x.Resolve(PaymentProvider.Fake))
            .Returns(Result<IPaymentGateway>.Success(gateway.Object));

        Payment? saved = null;
        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        repository.Setup(x => x.SaveAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((payment, _) => saved = payment)
            .ReturnsAsync(Result.Ok());

        var references = new Mock<IPaymentReferenceStore>();
        references.Setup(x => x.RecordAsync(
                It.IsAny<PaymentProvider>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var user = new Mock<Application.BuildingBlocks.Security.Authentication.Contexts.IUserContext>();
        user.SetupGet(x => x.UserId).Returns(CustomerId.ToString());

        var handler = new CreatePayment.UseCaseHandler(
            repository.Object,
            idempotency.Object,
            references.Object,
            registry.Object,
            new StubPaymentPolicy { HostedCheckoutIntentLifetime = TimeSpan.FromMinutes(15) },
            user.Object,
            TimeProvider.System);

        var result = await handler.Handle(
            new CreatePayment.UseCase(
                new CreatePayment.Request(
                    Usd(10m), PaymentProvider.Fake, "cart-life", PaymentCaptureMode.Automatic, null, null),
                "key-life"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        saved.Should().NotBeNull();
        saved!.ExpiresAt.Should().NotBeNull();
        saved.ExpiresAt!.Value.Should().BeCloseTo(
            DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Get_OverduePendingPayment_ShouldReadAsExpired()
    {
        Payment payment = PaymentFactory.Initiate(
            Guid.NewGuid(),
            PaymentProvider.Fake,
            "fake_1",
            Usd(10m),
            PaymentCaptureMode.Automatic,
            "cart-old",
            "key-old",
            "fp",
            CustomerId,
            null,
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow).Value;

        var store = new Mock<IPaymentReadStore>();
        store.Setup(x => x.FindAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentReadModel>.Fail(PaymentsOutcome.Failure.NotFound(payment.Id)));

        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        repository.Setup(x => x.LoadAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Payment>.Success(payment));

        var handler = new GetPayment.UseCaseHandler(
            store.Object, repository.Object, new StubPaymentPolicy(), TimeProvider.System);

        var result = await handler.Handle(
            new GetPayment.UseCase(payment.Id, CustomerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.Expired.ToWire());
        result.Value.ApprovalUrl.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithAnUnconfiguredCurrency_ShouldFail()
    {
        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        var idempotency = new Mock<IPaymentIdempotencyStore>();
        var references = new Mock<IPaymentReferenceStore>();
        var registry = new Mock<IPaymentGatewayRegistry>();
        var user = new Mock<Application.BuildingBlocks.Security.Authentication.Contexts.IUserContext>();
        user.SetupGet(x => x.UserId).Returns(CustomerId.ToString());

        var handler = new CreatePayment.UseCaseHandler(
            repository.Object,
            idempotency.Object,
            references.Object,
            registry.Object,
            new StubPaymentPolicy(),
            user.Object,
            TimeProvider.System);

        var request = new CreatePayment.Request(
            new Money(10m, new Currency("JPY")),
            PaymentProvider.Fake,
            "cart-jpy",
            PaymentCaptureMode.Automatic,
            null,
            null);

        var result = await handler.Handle(
            new CreatePayment.UseCase(request, "key-jpy"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.CurrencyUnsupported);
        registry.Verify(x => x.Resolve(It.IsAny<PaymentProvider>()), Times.Never);
    }

    [Fact]
    public async Task Create_BelowTheCurrencyCeiling_ShouldSucceed()
    {
        Guid paymentId = Guid.NewGuid();

        var idempotency = new Mock<IPaymentIdempotencyStore>();
        idempotency.Setup(x => x.ReserveAsync(CustomerId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IdempotencyReservation>.Success(
                new IdempotencyReservation(paymentId, IsNew: true, FingerprintMatches: true)));

        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(x => x.Provider).Returns(PaymentProvider.Fake);
        gateway.Setup(x => x.CreateIntentAsync(It.IsAny<PaymentIntentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaymentIntentResult>.Success(new PaymentIntentResult(
                "fake_1", PaymentProviderIntentStatus.RequiresAction, null, null, null, null)));

        var registry = new Mock<IPaymentGatewayRegistry>();
        registry.Setup(x => x.Resolve(PaymentProvider.Fake))
            .Returns(Result<IPaymentGateway>.Success(gateway.Object));

        var repository = new Mock<IEventSourcedRepository<Payment, Guid>>();
        repository.Setup(x => x.SaveAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var references = new Mock<IPaymentReferenceStore>();
        var user = new Mock<Application.BuildingBlocks.Security.Authentication.Contexts.IUserContext>();
        user.SetupGet(x => x.UserId).Returns(CustomerId.ToString());

        var policy = new StubPaymentPolicy
        {
            MaximumAmountsByCurrency = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["USD"] = 50m,
                ["EUR"] = 40m
            }
        };

        var handler = new CreatePayment.UseCaseHandler(
            repository.Object, idempotency.Object, references.Object, registry.Object, policy, user.Object, TimeProvider.System);

        var request = new CreatePayment.Request(
            new Money(40m, new Currency("EUR")), PaymentProvider.Fake, "cart-eur", PaymentCaptureMode.Automatic, null, null);

        var result = await handler.Handle(
            new CreatePayment.UseCase(request, "key-eur"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Currency.Should().Be("EUR");
    }
}
