using Application.Common.Querying.Mapping;

namespace Application.Tests.Common.Querying.Mapping;

public sealed class QueryValueConverterTest
{
    [Fact]
    public void Delegate_CanBeCreatedFromLambda()
    {
        QueryValueConverter converter = raw =>
            Result<QueryValue>.Success(new QueryValue(int.Parse(raw)));

        Result<QueryValue> result = converter("42");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(42);
    }

    [Fact]
    public void Delegate_CanReturnFailure()
    {
        QueryValueConverter converter = raw =>
            Result<QueryValue>.Fail(Error.BadRequest("Test.Invalid", "Invalid"));

        Result<QueryValue> result = converter("invalid");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Test.Invalid");
    }

    [Fact]
    public void Delegate_WithComplexLogic_Works()
    {
        QueryValueConverter converter = raw =>
        {
            if (raw.StartsWith("prefix:"))
                return Result<QueryValue>.Success(new QueryValue(raw[7..]));
            return Result<QueryValue>.Fail(Error.BadRequest("Test.Invalid", "Must have prefix"));
        };

        Result<QueryValue> success = converter("prefix:value");
        Result<QueryValue> failure = converter("noprefix");

        success.IsSuccess.Should().BeTrue();
        success.Value.Value.Should().Be("value");
        failure.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void NullConverter_ThrowsWhenInvoked()
    {
        QueryValueConverter? converter = null;

        Action act = () => converter!("test");

        act.Should().Throw<NullReferenceException>();
    }
}