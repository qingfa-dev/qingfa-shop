using AwesomeAssertions;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.UnitTests.Persistence.EntityFramework.QueryTranslation.EfCore;

[Trait("Category", "Unit")]
public sealed class EfOperatorRegistryTest
{
    [Fact(DisplayName = "DefaultRegistry_ContainsNoOperators")]
    public void DefaultRegistry_ContainsNoOperators()
    {
        var registry = new EfOperatorRegistry();

        registry.Contains("anyOp").Should().BeFalse();
    }

    [Fact(DisplayName = "Register_AddsOperator")]
    public void Register_AddsOperator()
    {
        var registry = new EfOperatorRegistry()
            .Register("testOp", (m, v) => Expression.Equal(m, v));

        registry.Contains("testOp").Should().BeTrue();
    }

    [Fact(DisplayName = "Build_WithRegisteredOperator_ReturnsExpression")]
    public void Build_WithRegisteredOperator_ReturnsExpression()
    {
        var registry = new EfOperatorRegistry()
            .Register("testOp", (m, v) => Expression.Equal(m, v));

        var member = Expression.Parameter(typeof(string), "x");
        var value = Expression.Constant("test");

        var result = registry.Build("testOp", member, value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<BinaryExpression>();
    }

    [Fact(DisplayName = "Build_WithUnregisteredOperator_ReturnsError")]
    public void Build_WithUnregisteredOperator_ReturnsError()
    {
        var registry = new EfOperatorRegistry();

        var result = registry.Build("unknown", Expression.Parameter(typeof(string), "x"), Expression.Constant("test"));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Operator.Unsupported");
    }

    [Fact(DisplayName = "Register_DoesNotOverwriteExistingOperator")]
    public void Register_DoesNotOverwriteExistingOperator()
    {
        var registry = new EfOperatorRegistry()
            .Register("testOp", (m, v) => Expression.Equal(m, v))
            .Register("testOp", (m, v) => Expression.NotEqual(m, v));

        var member = Expression.Parameter(typeof(string), "x");
        var value = Expression.Constant("test");
        var result = registry.Build("testOp", member, value);

        // First registration should win
        result.Value.Should().BeOfType<BinaryExpression>();
        ((BinaryExpression)result.Value).NodeType.Should().Be(ExpressionType.Equal);
    }

    [Fact(DisplayName = "ChainedRegistration_ReturnsSameRegistry")]
    public void ChainedRegistration_ReturnsSameRegistry()
    {
        var registry = new EfOperatorRegistry();
        var result = registry.Register("op1", (m, v) => Expression.Equal(m, v))
            .Register("op2", (m, v) => Expression.Equal(m, v));

        result.Should().BeSameAs(registry);
    }
}