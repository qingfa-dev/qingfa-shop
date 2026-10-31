using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Intents.Queries;
using Application.Modules.Payments.Persistence;

using AwesomeAssertions;

using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Moq;

namespace Application.Unit.Tests.Modules.Payments.Queries;

public sealed class GetPaymentTest
{
    [Fact]
    public async Task Handle_OtherCustomersPayment_ShouldReturnNotFound()
    {
        var row = new PaymentReadModel
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Status = "pending",
            Currency = PaymentConstant.Defaults.Currency
        };

        var store = new Mock<IPaymentReadStore>();
        store.Setup(x => x.FindAsync(row.Id, It.IsAny<CancellationToken>())).ReturnsAsync(row);

        var handler = new GetPayment.UseCaseHandler(store.Object);
        var query = new GetPayment.UseCase(row.Id, Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.NotFound);
    }

    [Fact]
    public async Task Handle_AdminRead_ShouldIgnoreOwnership()
    {
        var row = new PaymentReadModel
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Status = "pending",
            Currency = PaymentConstant.Defaults.Currency
        };

        var store = new Mock<IPaymentReadStore>();
        store.Setup(x => x.FindAsync(row.Id, It.IsAny<CancellationToken>())).ReturnsAsync(row);

        var handler = new GetPayment.UseCaseHandler(store.Object);
        var query = new GetPayment.UseCase(row.Id, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(row.Id);
    }
}
