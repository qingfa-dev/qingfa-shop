using Application.Modules.Payments.Events;

using AwesomeAssertions;

using Infrastructure.BuildingBlocks.Adapters.Messaging.Serialization;
using Infrastructure.Modules.Payments.Integrations.Messaging;
using Infrastructure.Modules.Payments.Integrations.Messaging.Handlers;

using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace Infrastructure.Unit.Tests.Modules.Payments;

/// <summary>Covers the payment messaging module and its consumer seam.</summary>
public sealed class PaymentMessagingTest
{
    [Fact]
    public void RegisterEventTypes_ShouldRegisterEveryPaymentContract()
    {
        var registry = new EventTypeRegistry();

        new PaymentMessagingModule().RegisterEventTypes(registry);

        // Enforce: the registry resolves each contract by its wire name and version.
        registry.Resolve(PaymentEventConstant.Captured, PaymentEventConstant.Version)
            .Should().Be<PaymentCapturedIntegration>();
        registry.Resolve(PaymentEventConstant.Refunded, PaymentEventConstant.Version)
            .Should().Be<PaymentRefundedIntegration>();
        registry.Resolve(PaymentEventConstant.Failed, PaymentEventConstant.Version)
            .Should().Be<PaymentFailedIntegration>();
    }

    [Fact]
    public void Configure_WithInMemoryTransport_ShouldNotThrow()
    {
        var registry = new EventTypeRegistry();
        var module = new PaymentMessagingModule();

        Action act = () => module.Configure(
            new Wolverine.WolverineOptions(),
            new Infrastructure.BuildingBlocks.Adapters.Messaging.Options.MessagingSetting(),
            registry,
            transport: null);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task Consumer_ShouldRecordTheDeliveryOnce()
    {
        var inbox = new Mock<Application.BuildingBlocks.Messaging.Inbox.IInboxStore>();
        inbox.Setup(x => x.ExecuteOnceAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<Result>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Guid, string, Func<CancellationToken, Task<Result>>, CancellationToken>(
                (_, _, handler, token) => handler(token));

        var publisher = new Mock<MediatR.IPublisher>();
        var handler = new PaymentCapturedIntegrationHandler(inbox.Object, publisher.Object);

        await handler.Handle(
            new PaymentCapturedIntegration { PaymentId = Guid.NewGuid(), Amount = 10m, Currency = "USD" },
            CancellationToken.None);

        inbox.Verify(x => x.ExecuteOnceAsync(
            It.IsAny<Guid>(),
            It.Is<string>(consumer => consumer.Contains(nameof(PaymentCapturedIntegrationHandler), StringComparison.Ordinal)),
            It.IsAny<Func<CancellationToken, Task<Result>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
