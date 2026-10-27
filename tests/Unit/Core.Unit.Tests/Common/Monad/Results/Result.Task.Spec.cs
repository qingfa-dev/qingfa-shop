namespace Core.Tests.Common.Monad.Results;

/// <summary>Tests for the extensions that continue an already-started result chain.</summary>
[Trait("Category", "Unit")]
public class ResultTaskSpec
{
    #region Awaitable Bind

    [Fact]
    public async Task BindOnTask_WhenSuccess_ShouldPassTheValueToTheNextStep()
    {
        Task<Result<int>> chain = Task.FromResult(Result<int>.Ok(4));

        Result<string> bound = await chain.BindAsync(value => Result<string>.Ok($"value-{value}"));

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("value-4");
    }

    [Fact]
    public async Task BindOnTask_WhenFailure_ShouldShortCircuit()
    {
        Task<Result<int>> chain = Task.FromResult(
            Result<int>.Fail(ResultStub.NotFoundError()));

        bool invoked = false;
        Result<string> bound = await chain.BindAsync(value =>
        {
            invoked = true;
            return Result<string>.Ok(value.ToString());
        });

        invoked.Should().BeFalse();
        bound.IsFailure.Should().BeTrue();
        bound.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
    }

    [Fact]
    public async Task BindAsyncOnTask_WhenSuccess_ShouldAwaitTheNextStep()
    {
        Task<Result<int>> chain = Task.FromResult(Result<int>.Ok(7));

        Result<string> bound = await chain.BindAsync(
            (value, _) => Task.FromResult(Result<string>.Ok(value.ToString())),
            TestContext.Current.CancellationToken);

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("7");
    }

    [Fact]
    public async Task BindAsyncOnTask_WhenFailure_ShouldShortCircuit()
    {
        Task<Result<int>> chain = Task.FromResult(
            Result<int>.Fail(ResultStub.NotFoundError()));

        bool invoked = false;
        Result<string> bound = await chain.BindAsync(
            (value, _) =>
            {
                invoked = true;
                return Task.FromResult(Result<string>.Ok(value.ToString()));
            },
            TestContext.Current.CancellationToken);

        invoked.Should().BeFalse();
        bound.IsFailure.Should().BeTrue();
        bound.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
    }

    #endregion

    #region Awaitable Map

    [Fact]
    public async Task MapOnTask_WhenSuccess_ShouldProjectTheValue()
    {
        Task<Result<int>> chain = Task.FromResult(Result<int>.Ok(3));

        Result<string> mapped = await chain.MapAsync(value => $"value-{value}");

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("value-3");
    }

    [Fact]
    public async Task MapOnTask_WhenFailure_ShouldShortCircuit()
    {
        Task<Result<int>> chain = Task.FromResult(
            Result<int>.Fail(ResultStub.NotFoundError()));

        bool invoked = false;
        Result<string> mapped = await chain.MapAsync(value =>
        {
            invoked = true;
            return value.ToString();
        });

        invoked.Should().BeFalse();
        mapped.IsFailure.Should().BeTrue();
        mapped.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
    }

    #endregion

    #region Then

    [Fact]
    public async Task ThenAsync_WhenSuccess_ShouldKeepTheCurrentValue()
    {
        Result<int> current = Result<int>.Ok(5);

        Result<int> continued = await current.ThenAsync(
            (_, _) => Task.FromResult(Result.Success()),
            TestContext.Current.CancellationToken);

        continued.IsSuccess.Should().BeTrue();
        continued.Value.Should().Be(5);
    }

    [Fact]
    public async Task ThenAsync_WhenStepFails_ShouldPropagateFailure()
    {
        Result<int> current = Result<int>.Ok(5);

        Result<int> continued = await current.ThenAsync(
            (_, _) => Task.FromResult(Result.Fail(ResultStub.NotFoundError())),
            TestContext.Current.CancellationToken);

        continued.IsFailure.Should().BeTrue();
        continued.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
    }

    [Fact]
    public async Task ThenAsync_WhenCurrentFails_ShouldShortCircuit()
    {
        Result<int> current = Result<int>.Fail(ResultStub.NotFoundError());

        bool invoked = false;
        Result<int> continued = await current.ThenAsync(
            (_, _) =>
            {
                invoked = true;
                return Task.FromResult(Result.Success());
            },
            TestContext.Current.CancellationToken);

        invoked.Should().BeFalse();
        continued.IsFailure.Should().BeTrue();
        continued.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
    }

    [Fact]
    public async Task ThenAsyncOnTask_WhenSuccess_ShouldKeepTheCurrentValue()
    {
        Task<Result<int>> chain = Task.FromResult(Result<int>.Ok(5));

        Result<int> continued = await chain.ThenAsync(
            (_, _) => Task.FromResult(Result.Success()),
            TestContext.Current.CancellationToken);

        continued.IsSuccess.Should().BeTrue();
        continued.Value.Should().Be(5);
    }

    [Fact]
    public async Task ThenAsyncOnTask_WhenStepFails_ShouldPropagateFailure()
    {
        Task<Result<int>> chain = Task.FromResult(Result<int>.Ok(5));

        Result<int> continued = await chain.ThenAsync(
            (_, _) => Task.FromResult(Result.Fail(ResultStub.NotFoundError())),
            TestContext.Current.CancellationToken);

        continued.IsFailure.Should().BeTrue();
        continued.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
    }

    #endregion
}
