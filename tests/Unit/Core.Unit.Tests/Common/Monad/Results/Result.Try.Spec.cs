namespace Core.Unit.Tests.Common.Monad.Results;

/// <summary>Tests for Try, MapTry, and BindTry extensions in <see cref="ResultExtension"/>.</summary>
[Trait("Category", "Unit")]
public class ResultTrySpec
{
    #region Try

    /// <summary>Non-generic Try should guard callbacks and convert exceptions.</summary>
    [Fact]
    public void Try_ShouldGuardCallbacksAndConvertExceptions()
    {
        // Act & Assert
        ResultExtension.Try(null).Errors[0].Code.Should().Be("result.flow.callback_missing");

        ResultExtension.Try(() => null!).Errors[0].Code.Should().Be("result.flow.result_missing");

        ResultExtension.Try(() => Result.Ok()).IsSuccess.Should().BeTrue();

        var mapped = ResultExtension.Try(
            (Func<Result>)(() => throw new InvalidOperationException("boom")));
        mapped.IsFailure.Should().BeTrue();
        mapped.Errors[0].Code.Should().Be("result.exception");

        var factoryMapped = ResultExtension.Try(
            (Func<Result>)(() => throw new InvalidOperationException("boom")),
            _ => ResultStub.ConflictError());
        factoryMapped.Errors[0].Code.Should().Be("test.conflict");

        var nullMapped = ResultExtension.Try(
            (Func<Result>)(() => throw new InvalidOperationException("boom")),
            _ => null!);
        nullMapped.Errors[0].Code.Should().Be("result.flow.exception_mapping_failed");

        Action cancelled = () => ResultExtension.Try(
            (Func<Result>)(() => throw new OperationCanceledException()));
        cancelled.Should().Throw<OperationCanceledException>();
    }

    /// <summary>Generic Try should guard callbacks and convert exceptions.</summary>
    [Fact]
    public void GenericTry_ShouldGuardCallbacksAndConvertExceptions()
    {
        // Act & Assert
        ResultExtension.Try<int>(null).Errors[0].Code.Should().Be("result.flow.callback_missing");

        ResultExtension.Try<int>(() => null!).Errors[0].Code.Should().Be("result.flow.result_missing");

        ResultExtension.Try<int>(() => Result<int>.Ok(5)).Value.Should().Be(5);

        var mapped = ResultExtension.Try<int>(
            () => throw new InvalidOperationException("boom"));
        mapped.IsFailure.Should().BeTrue();
        mapped.Errors[0].Code.Should().Be("result.exception");

        var factoryMapped = ResultExtension.Try<int>(
            () => throw new InvalidOperationException("boom"),
            _ => ResultStub.ConflictError());
        factoryMapped.Errors[0].Code.Should().Be("test.conflict");

        var nullMapped = ResultExtension.Try<int>(
            () => throw new InvalidOperationException("boom"),
            _ => null!);
        nullMapped.Errors[0].Code.Should().Be("result.flow.exception_mapping_failed");

        Action cancelled = () => ResultExtension.Try<int>(
            () => throw new OperationCanceledException());
        cancelled.Should().Throw<OperationCanceledException>();
    }

    #endregion

    #region MapTry and BindTry

    /// <summary>MapTry should propagate failure, guard callbacks and convert exceptions.</summary>
    [Fact]
    public void MapTry_ShouldPropagateFailureGuardCallbacksAndConvertExceptions()
    {
        // Arrange
        var failure = Result<int>.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");
        var success = Result<int>.Ok(3)
            .WithMetadata("source", "kept");

        // Act & Assert
        var propagated = failure.MapTry(v => v * 2);
        propagated.IsFailure.Should().BeTrue();
        propagated.Metadata["source"].Should().Be("kept");

        success.MapTry((Func<int, int>?)null)
            .Errors[0].Code.Should().Be("result.flow.callback_missing");

        success.MapTry(v => v * 2).Value.Should().Be(6);

        success.MapTry<int, int>(_ => throw new InvalidOperationException())
            .Errors[0].Code.Should().Be("result.exception");

        success.MapTry<int, int>(
                _ => throw new InvalidOperationException(),
                _ => ResultStub.ConflictError())
            .Errors[0].Code.Should().Be("test.conflict");

        Action cancelled = () => success.MapTry<int, int>(
            _ => throw new OperationCanceledException());
        cancelled.Should().Throw<OperationCanceledException>();
    }

    /// <summary>BindTry should propagate failure, guard callbacks and convert exceptions.</summary>
    [Fact]
    public void BindTry_ShouldPropagateFailureGuardCallbacksAndConvertExceptions()
    {
        // Arrange
        var failure = Result<int>.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");
        var success = Result<int>.Ok(3)
            .WithMetadata("source", "kept");

        // Act & Assert
        var propagated = failure.BindTry(v => Result<string>.Ok(v.ToString()));
        propagated.IsFailure.Should().BeTrue();
        propagated.Metadata["source"].Should().Be("kept");

        success.BindTry((Func<int, Result<string>>?)null)
            .Errors[0].Code.Should().Be("result.flow.callback_missing");

        success.BindTry<int, string>(_ => null!)
            .Errors[0].Code.Should().Be("result.flow.result_missing");

        success.BindTry(v => Result<string>.Ok(v.ToString())).Value.Should().Be("3");

        success.BindTry<int, string>(_ => throw new InvalidOperationException())
            .Errors[0].Code.Should().Be("result.exception");

        success.BindTry<int, string>(
                _ => throw new InvalidOperationException(),
                _ => ResultStub.ConflictError())
            .Errors[0].Code.Should().Be("test.conflict");

        Action cancelled = () => success.BindTry<int, string>(
            _ => throw new OperationCanceledException());
        cancelled.Should().Throw<OperationCanceledException>();
    }

    #endregion

    #region TryAsync Generic

    /// <summary>TryAsync legacy overload should guard callbacks and convert exceptions.</summary>
    [Fact]
    public async Task TryAsyncGenericLegacy_ShouldGuardAndConvert()
    {
        var token = TestContext.Current.CancellationToken;

        (await ResultExtension.TryAsync<int>((Func<Task<int>>?)null, null, token))
            .Errors[0].Code.Should().Be("result.flow.callback_missing");

        Func<Task<int>> noTask = () => null!;
        (await noTask.TryAsync(null, token))
            .Errors[0].Code.Should().Be("result.flow.task_missing");

        Func<Task<int>> success = () => Task.FromResult(5);
        (await success.TryAsync(null, token)).Value.Should().Be(5);

        Func<Task<int>> throws = () => throw new InvalidOperationException();
        (await throws.TryAsync(null, token))
            .Errors[0].Code.Should().Be("result.exception");

        (await throws.TryAsync(_ => ResultStub.ConflictError(), token))
            .Errors[0].Code.Should().Be("test.conflict");

        (await throws.TryAsync(_ => null!, token))
            .Errors[0].Code.Should().Be("result.flow.exception_mapping_failed");

        Func<Task<int>> cancelled = () => throw new OperationCanceledException();
        Func<Task<Result<int>>> act = () => cancelled.TryAsync(null, token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>TryAsync token-aware overload should forward the token.</summary>
    [Fact]
    public async Task TryAsyncGenericTokenAware_ShouldForwardToken()
    {
        var token = TestContext.Current.CancellationToken;
        var received = CancellationToken.None;
        var called = false;

        var result = await ((Func<CancellationToken, Task<int>>)(async ct =>
        {
            called = true;
            received = ct;
            await Task.Yield();
            return 5;
        })).TryAsync(null, token);

        result.Value.Should().Be(5);
        called.Should().BeTrue();
        received.Should().Be(token);

        (await ((Func<CancellationToken, Task<int>>)(_ => null!)).TryAsync(null, token))
            .Errors[0].Code.Should().Be("result.flow.task_missing");
    }

    #endregion

    #region TryAsync Non-Generic

    /// <summary>TryAsync non-generic legacy overload should guard callbacks and convert exceptions.</summary>
    [Fact]
    public async Task TryAsyncNonGenericLegacy_ShouldGuardAndConvert()
    {
        var token = TestContext.Current.CancellationToken;

        (await ResultExtension.TryAsync((Func<Task>?)null, null, token))
            .Errors[0].Code.Should().Be("result.flow.callback_missing");

        Func<Task> noTask = () => null!;
        (await noTask.TryAsync(null, token))
            .Errors[0].Code.Should().Be("result.exception");

        var completed = false;
        Func<Task> success = () =>
        {
            completed = true;
            return Task.CompletedTask;
        };
        (await success.TryAsync(null, token)).IsSuccess.Should().BeTrue();
        completed.Should().BeTrue();

        Func<Task> throws = () => throw new InvalidOperationException();
        (await throws.TryAsync(null, token))
            .Errors[0].Code.Should().Be("result.exception");

        (await throws.TryAsync(_ => ResultStub.ConflictError(), token))
            .Errors[0].Code.Should().Be("test.conflict");

        (await throws.TryAsync(_ => null!, token))
            .Errors[0].Code.Should().Be("result.flow.exception_mapping_failed");

        Func<Task> cancelled = () => throw new OperationCanceledException();
        Func<Task<Result>> act = () => cancelled.TryAsync(null, token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>TryAsync non-generic token-aware overload should forward the token.</summary>
    [Fact]
    public async Task TryAsyncNonGenericTokenAware_ShouldForwardToken()
    {
        var token = TestContext.Current.CancellationToken;
        var received = CancellationToken.None;

        var result = await ((Func<CancellationToken, Task>)(async ct =>
        {
            received = ct;
            await Task.Yield();
        })).TryAsync(null, token);

        result.IsSuccess.Should().BeTrue();
        received.Should().Be(token);

        (await ((Func<CancellationToken, Task>)(_ => null!)).TryAsync(null, token))
            .Errors[0].Code.Should().Be("result.flow.task_missing");
    }

    #endregion
}
