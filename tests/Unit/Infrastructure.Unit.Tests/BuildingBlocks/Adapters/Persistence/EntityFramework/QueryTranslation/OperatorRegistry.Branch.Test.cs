using System.Linq.Expressions;

using Application.BuildingBlocks.Querying;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

/// <summary>Branch tests for the custom operator registry.</summary>
[Trait("Category", "Unit")]
public sealed class OperatorRegistryBranchTest
{
    [Theory(DisplayName = "Register should reject a null or whitespace name")]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithInvalidName_ShouldThrow(string name)
    {
        var registry = new EfOperatorRegistry();

        Action act = () => registry.Register(name, (member, value) => member);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Register should reject a null builder")]
    public void Register_WithNullBuilder_ShouldThrow()
    {
        var registry = new EfOperatorRegistry();

        Action act = () => registry.Register("op", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory(DisplayName = "Contains should return false for missing or empty names")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("missing")]
    public void Contains_WithMissingName_ShouldReturnFalse(string name)
    {
        var registry = new EfOperatorRegistry();

        registry.Contains(name).Should().BeFalse();
    }

    [Fact(DisplayName = "Contains should be case-insensitive")]
    public void Contains_WithDifferentCase_ShouldReturnTrue()
    {
        var registry = new EfOperatorRegistry()
            .Register("custom", (member, value) => member);

        registry.Contains("CUSTOM").Should().BeTrue();
    }

    [Theory(DisplayName = "Build should reject missing or empty names")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("missing")]
    public void Build_WithMissingName_ShouldFail(string name)
    {
        var registry = new EfOperatorRegistry();
        ParameterExpression member = Expression.Parameter(typeof(string), "x");
        ConstantExpression value = Expression.Constant("v");

        Result<Expression> result = registry.Build(name, member, value);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.OperatorUnsupported);
    }

    [Fact(DisplayName = "Build should reject a null operator name")]
    public void Build_WithNullName_ShouldFail()
    {
        var registry = new EfOperatorRegistry();
        ParameterExpression member = Expression.Parameter(typeof(string), "x");
        ConstantExpression value = Expression.Constant("v");

        Result<Expression> result = registry.Build(null!, member, value);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.OperatorUnsupported);
    }

    [Fact(DisplayName = "Build should return the expression produced by a registered builder")]
    public void Build_WithRegisteredBuilder_ShouldReturnExpression()
    {
        var registry = new EfOperatorRegistry()
            .Register("equals", Expression.Equal);
        ParameterExpression member = Expression.Parameter(typeof(string), "x");
        ConstantExpression value = Expression.Constant("v");

        Result<Expression> result = registry.Build("equals", member, value);

        result.IsSuccess.Should().BeTrue();
        result.Value.NodeType.Should().Be(ExpressionType.Equal);
    }

    [Theory(DisplayName = "Build should translate builder argument failures to unsupported operators")]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(ArgumentException))]
    public void Build_WhenBuilderThrowsKnownFailure_ShouldReturnUnsupported(Type exceptionType)
    {
        var registry = new EfOperatorRegistry()
            .Register("boom", (_, _) => throw (Exception)Activator.CreateInstance(exceptionType)!);
        ParameterExpression member = Expression.Parameter(typeof(string), "x");
        ConstantExpression value = Expression.Constant("v");

        Result<Expression> result = registry.Build("boom", member, value);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.OperatorUnsupported);
    }
}
