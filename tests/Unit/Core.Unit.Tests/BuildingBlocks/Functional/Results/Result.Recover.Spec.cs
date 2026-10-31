namespace Core.Unit.Tests.BuildingBlocks.Functional.Results;

/// <summary>Tests for Recover and OrElse extensions in <see cref="ResultExtension"/>.</summary>
[Trait("Category", "Unit")]
public class ResultRecoverSpec
{
    #region Recover

    /// <summary>Non-generic Recover should pass success and recover failures.</summary>
    [Fact]
    public void Recover_ShouldPassSuccessAndRecoverFailures()
    {
        // Arrange
        var success = Result.Ok();
        var failure = Result.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");

        // Act & Assert
        failure.Recover(null).Errors[0].Code.Should().Be("result.flow.callback_missing");

        success.Recover(_ => null!).Should().BeSameAs(success);

        var nullRecovered = failure.Recover(_ => null!);
        nullRecovered.Errors[0].Code.Should().Be("result.flow.result_missing");

        var recovered = failure.Recover(_ => Result.Ok());
        recovered.IsSuccess.Should().BeTrue();
        recovered.Metadata["source"].Should().Be("kept");
    }

    /// <summary>Generic Recover should pass success and recover failures.</summary>
    [Fact]
    public void GenericRecover_ShouldPassSuccessAndRecoverFailures()
    {
        // Arrange
        var success = Result<int>.Ok(1);
        var failure = Result<int>.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");

        // Act & Assert
        failure.Recover(null).Errors[0].Code.Should().Be("result.flow.callback_missing");

        success.Recover(_ => null!).Should().BeSameAs(success);

        var nullRecovered = failure.Recover(_ => null!);
        nullRecovered.Errors[0].Code.Should().Be("result.flow.result_missing");

        var recovered = failure.Recover(_ => Result<int>.Ok(42));
        recovered.IsSuccess.Should().BeTrue();
        recovered.Value.Should().Be(42);
        recovered.Metadata["source"].Should().Be("kept");
    }

    #endregion

    #region OrElse

    /// <summary>Non-generic OrElse should pass success and replace failures.</summary>
    [Fact]
    public void OrElse_ShouldPassSuccessAndReplaceFailures()
    {
        // Arrange
        var success = Result.Ok();
        var failure = Result.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");

        // Act & Assert
        failure.OrElse(null).Errors[0].Code.Should().Be("result.flow.callback_missing");

        success.OrElse(null!).Should().BeSameAs(success);

        var nullFallback = failure.OrElse(() => null!);
        nullFallback.Errors[0].Code.Should().Be("result.flow.result_missing");

        var replaced = failure.OrElse(() => Result.Ok());
        replaced.IsSuccess.Should().BeTrue();
        replaced.Metadata["source"].Should().Be("kept");
    }

    /// <summary>Generic OrElse should pass success and replace failures.</summary>
    [Fact]
    public void GenericOrElse_ShouldPassSuccessAndReplaceFailures()
    {
        // Arrange
        var success = Result<int>.Ok(1);
        var failure = Result<int>.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");

        // Act & Assert
        failure.OrElse(null).Errors[0].Code.Should().Be("result.flow.callback_missing");

        success.OrElse(null!).Should().BeSameAs(success);

        var nullFallback = failure.OrElse(() => null!);
        nullFallback.Errors[0].Code.Should().Be("result.flow.result_missing");

        var replaced = failure.OrElse(() => Result<int>.Ok(7));
        replaced.IsSuccess.Should().BeTrue();
        replaced.Value.Should().Be(7);
        replaced.Metadata["source"].Should().Be("kept");
    }

    #endregion

    #region RecoverAsync

    /// <summary>RecoverAsync legacy overload should pass success and recover failures.</summary>
    [Fact]
    public async Task RecoverAsyncLegacy_ShouldPassSuccessAndRecover()
    {
        var token = TestContext.Current.CancellationToken;
        var success = Result<int>.Ok(1);
        var failure = Result<int>.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");

        (await success.RecoverAsync<int>((Func<List<Error>, Task<int>>?)null, token))
            .Should().BeSameAs(success);

        (await failure.RecoverAsync<int>((Func<List<Error>, Task<int>>?)null, token))
            .Errors[0].Code.Should().Be("result.flow.callback_missing");

        (await failure.RecoverAsync<int>(_ => null!, token))
            .Errors[0].Code.Should().Be("result.flow.task_missing");

        var recovered = await failure.RecoverAsync(_ => Task.FromResult(42), token);
        recovered.IsSuccess.Should().BeTrue();
        recovered.Value.Should().Be(42);
        recovered.Metadata["source"].Should().Be("kept");
    }

    /// <summary>RecoverAsync token-aware overload should forward the token.</summary>
    [Fact]
    public async Task RecoverAsyncTokenAware_ShouldForwardToken()
    {
        var token = TestContext.Current.CancellationToken;
        var failure = Result<int>.Fail(ResultStub.NotFoundError());
        var received = CancellationToken.None;

        var recovered = await failure.RecoverAsync(
            (_, ct) =>
            {
                received = ct;
                return Task.FromResult(42);
            },
            token);

        recovered.Value.Should().Be(42);
        received.Should().Be(token);

        (await failure.RecoverAsync<int>(
            (Func<List<Error>, CancellationToken, Task<int>>?)null,
            token)).Errors[0].Code.Should().Be("result.flow.callback_missing");
    }

    #endregion

    #region OrElseAsync

    /// <summary>OrElseAsync legacy overload should pass success and replace failures.</summary>
    [Fact]
    public async Task OrElseAsyncLegacy_ShouldPassSuccessAndReplace()
    {
        var token = TestContext.Current.CancellationToken;
        var success = Result<int>.Ok(1);
        var failure = Result<int>.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");

        (await success.OrElseAsync<int>((Func<List<Error>, Task<Result<int>>>?)null, token))
            .Should().BeSameAs(success);

        (await failure.OrElseAsync<int>((Func<List<Error>, Task<Result<int>>>?)null, token))
            .Errors[0].Code.Should().Be("result.flow.callback_missing");

        (await failure.OrElseAsync<int>(_ => null!, token))
            .Errors[0].Code.Should().Be("result.flow.task_missing");

        var noResult = await failure.OrElseAsync(
            _ => Task.FromResult<Result<int>>(null!),
            token);
        noResult.Errors[0].Code.Should().Be("result.flow.result_missing");

        var replaced = await failure.OrElseAsync(
            _ => Task.FromResult(Result<int>.Ok(7)),
            token);
        replaced.IsSuccess.Should().BeTrue();
        replaced.Value.Should().Be(7);
        replaced.Metadata["source"].Should().Be("kept");
    }

    /// <summary>OrElseAsync token-aware overload should forward the token.</summary>
    [Fact]
    public async Task OrElseAsyncTokenAware_ShouldForwardToken()
    {
        var token = TestContext.Current.CancellationToken;
        var failure = Result<int>.Fail(ResultStub.NotFoundError());
        var received = CancellationToken.None;

        var replaced = await failure.OrElseAsync(
            (_, ct) =>
            {
                received = ct;
                return Task.FromResult(Result<int>.Ok(7));
            },
            token);

        replaced.Value.Should().Be(7);
        received.Should().Be(token);

        (await failure.OrElseAsync<int>(
            (Func<List<Error>, CancellationToken, Task<Result<int>>>?)null,
            token)).Errors[0].Code.Should().Be("result.flow.callback_missing");
    }

    #endregion
}
