using Core.BuildingBlocks.Domain.Policies;

namespace Core.Unit.Tests.BuildingBlocks.Domain.Policies;

/// <summary>Tests for Result-returning domain policies.</summary>
[Trait("Category", "Unit")]
public sealed class DomainPolicyTest
{
    private sealed record OrderContext(int Quantity, int Stock);

    private sealed class StockPolicy : DomainPolicy<OrderContext, int>
    {
        public override Result<int> Evaluate(OrderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return context.Quantity > 0 && context.Quantity <= context.Stock
                ? Result<int>.Success(context.Quantity)
                : Result<int>.Fail(Error.Validation(
                    "Order.Quantity.Invalid",
                    "The quantity must be positive and available in stock."));
        }
    }

    [Fact]
    public void Evaluate_WhenRulePasses_ReturnsValue()
    {
        // Arrange
        var policy = new StockPolicy();

        // Act
        Result<int> result = policy.Evaluate(new OrderContext(2, 5));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    [Fact]
    public void Evaluate_WhenRuleFails_ReturnsStableFailure()
    {
        // Arrange
        var policy = new StockPolicy();

        // Act
        Result<int> result = policy.Evaluate(new OrderContext(6, 5));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Order.Quantity.Invalid");
    }

    [Fact]
    public void Evaluate_WithNullContext_Throws()
    {
        // Arrange
        var policy = new StockPolicy();

        // Act
        Action act = () => policy.Evaluate(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}