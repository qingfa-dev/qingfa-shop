namespace Core.UnitTests.Common.Monad.Results;

/// <summary>Tests for the asynchronous flow extensions in <see cref="ResultExtension"/>.</summary>
[Trait("Category", "Unit")]
public class ResultFlowAsyncSpec
{
    #region MatchAsync and FoldAsync

    /// <summary>MatchAsync legacy overload should select the callback by state.</summary>
    [Fact]
    public async Task MatchAsyncLegacy_ShouldSelectCallbackByState()
    {
        var token = TestContext.Current.CancellationToken;
        var success = Result<int>.Ok(7);
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        var ok = await success.MatchAsync(
            v => Task.FromResult($"ok:{v}"),
            _ => Task.FromResult("fail"),
            token);
        ok.Should().Be("ok:7");

        var failed = await failure.MatchAsync(
            _ => Task.FromResult("ok"),
            errors => Task.FromResult(errors[0].Code),
            token);
        failed.Should().Be("test.not_found");

        (await success.MatchAsync<int, string>(
            (Func<int, Task<string>>?)null,
            _ => Task.FromResult("fail"),
            token)).Should().BeNull();

        (await failure.MatchAsync<int, string>(
            _ => Task.FromResult("ok"),
            (Func<List<Error>, Task<string>>?)null,
            token)).Should().BeNull();
    }

    /// <summary>MatchAsync legacy overload should default when a callback returns a null task.</summary>
    [Fact]
    public async Task MatchAsyncLegacy_ShouldDefault_WhenTaskIsNull()
    {
        var token = TestContext.Current.CancellationToken;
        var success = Result<int>.Ok(7);
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        (await success.MatchAsync<int, string>(
            _ => null!,
            _ => Task.FromResult("fail"),
            token)).Should().BeNull();

        (await failure.MatchAsync<int, string>(
            _ => Task.FromResult("ok"),
            _ => null!,
            token)).Should().BeNull();
    }

    /// <summary>MatchAsync token-aware overload should forward the token and select the callback.</summary>
    [Fact]
    public async Task MatchAsyncTokenAware_ShouldForwardTokenAndSelectCallback()
    {
        var token = TestContext.Current.CancellationToken;
        var success = Result<int>.Ok(7);
        var failure = Result<int>.Fail(ResultStub.NotFoundError());
        var received = CancellationToken.None;

        var ok = await success.MatchAsync(
            (v, ct) =>
            {
                received = ct;
                return Task.FromResult($"ok:{v}");
            },
            (_, ct) =>
            {
                received = ct;
                return Task.FromResult("fail");
            },
            token);
        ok.Should().Be("ok:7");
        received.Should().Be(token);

        var failed = await failure.MatchAsync(
            (_, ct) =>
            {
                received = ct;
                return Task.FromResult("ok");
            },
            (errors, ct) =>
            {
                received = ct;
                return Task.FromResult(errors[0].Code);
            },
            token);
        failed.Should().Be("test.not_found");
        received.Should().Be(token);

        (await success.MatchAsync<int, string>(
            (Func<int, CancellationToken, Task<string>>?)null,
            (_, _) => Task.FromResult("fail"),
            token)).Should().BeNull();

        (await failure.MatchAsync<int, string>(
            (_, _) => Task.FromResult("ok"),
            (Func<List<Error>, CancellationToken, Task<string>>?)null,
            token)).Should().BeNull();
    }

    /// <summary>FoldAsync should behave like MatchAsync.</summary>
    [Fact]
    public async Task FoldAsync_ShouldBehaveLikeMatchAsync()
    {
        var token = TestContext.Current.CancellationToken;
        var success = Result<int>.Ok(7);
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        (await success.FoldAsync(
            v => Task.FromResult(v * 2),
            _ => Task.FromResult(-1),
            token)).Should().Be(14);

        (await failure.FoldAsync(
            (v, _) => Task.FromResult(v * 2),
            (errors, _) => Task.FromResult(errors.Count),
            token)).Should().Be(1);
    }

    #endregion

    #region MapAsync

    /// <summary>MapAsync legacy overload should propagate failure, guard callbacks and map success.</summary>
    [Fact]
    public async Task MapAsyncLegacy_ShouldPropagateFailureGuardAndMap()
    {
        var token = TestContext.Current.CancellationToken;
        var failure = Result<int>.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");
        var success = Result<int>.Ok(7)
            .WithMetadata("source", "kept");

        var propagated = await failure.MapAsync(v => Task.FromResult(v + 1), token);
        propagated.IsFailure.Should().BeTrue();
        propagated.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        propagated.Metadata["source"].Should().Be("kept");

        var missing = await success.MapAsync<int, int>((Func<int, Task<int>>?)null, token);
        missing.Errors[0].Code.Should().Be("result.flow.callback_missing");

        var noTask = await success.MapAsync<int, int>(_ => null!, token);
        noTask.Errors[0].Code.Should().Be("result.flow.task_missing");

        var mapped = await success.MapAsync(v => Task.FromResult(v + 1), token);
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(8);
        mapped.Metadata["source"].Should().Be("kept");
    }

    /// <summary>MapAsync token-aware overload should forward the token.</summary>
    [Fact]
    public async Task MapAsyncTokenAware_ShouldForwardToken()
    {
        var token = TestContext.Current.CancellationToken;
        var success = Result<int>.Ok(7);
        var received = CancellationToken.None;

        var mapped = await success.MapAsync(
            (v, ct) =>
            {
                received = ct;
                return Task.FromResult(v + 1);
            },
            token);

        mapped.Value.Should().Be(8);
        received.Should().Be(token);

        (await success.MapAsync<int, int>(
            (Func<int, CancellationToken, Task<int>>?)null,
            token)).Errors[0].Code.Should().Be("result.flow.callback_missing");
    }

    #endregion

    #region BindAsync

    /// <summary>BindAsync legacy overload should propagate failure, guard callbacks and bind success.</summary>
    [Fact]
    public async Task BindAsyncLegacy_ShouldPropagateFailureGuardAndBind()
    {
        var token = TestContext.Current.CancellationToken;
        var failure = Result<int>.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");
        var success = Result<int>.Ok(7)
            .WithMetadata("source", "kept");

        var propagated = await failure.BindAsync(
            v => Task.FromResult(Result<string>.Ok(v.ToString())),
            token);
        propagated.IsFailure.Should().BeTrue();
        propagated.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        propagated.Metadata["source"].Should().Be("kept");

        var missing = await success.BindAsync<int, string>((Func<int, Task<Result<string>>>?)null, token);
        missing.Errors[0].Code.Should().Be("result.flow.callback_missing");

        var noTask = await success.BindAsync<int, string>(_ => null!, token);
        noTask.Errors[0].Code.Should().Be("result.flow.task_missing");

        var noResult = await success.BindAsync(
            _ => Task.FromResult<Result<string>>(null!),
            token);
        noResult.Errors[0].Code.Should().Be("result.flow.result_missing");

        var bound = await success.BindAsync(
            v => Task.FromResult(Result<string>.Ok($"value-{v}")),
            token);
        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("value-7");
        bound.Metadata["source"].Should().Be("kept");
    }

    /// <summary>BindAsync token-aware overload should forward the token.</summary>
    [Fact]
    public async Task BindAsyncTokenAware_ShouldForwardToken()
    {
        var token = TestContext.Current.CancellationToken;
        var success = Result<int>.Ok(7);
        var received = CancellationToken.None;

        var bound = await success.BindAsync(
            (v, ct) =>
            {
                received = ct;
                return Task.FromResult(Result<string>.Ok(v.ToString()));
            },
            token);

        bound.Value.Should().Be("7");
        received.Should().Be(token);

        (await success.BindAsync<int, string>(
            (Func<int, CancellationToken, Task<Result<string>>>?)null,
            token)).Errors[0].Code.Should().Be("result.flow.callback_missing");
    }

    #endregion

    #region EnsureAsync

    /// <summary>EnsureAsync legacy overload should pass failures and enforce the predicate.</summary>
    [Fact]
    public async Task EnsureAsyncLegacy_ShouldPassFailuresAndEnforcePredicate()
    {
        var token = TestContext.Current.CancellationToken;
        var failure = Result<int>.Fail(ResultStub.NotFoundError());
        var success = Result<int>.Ok(7).WithMetadata("source", "kept");
        var guard = ResultStub.ConflictError();

        (await failure.EnsureAsync<int>((Func<int, Task<bool>>?)null, guard, token))
            .Should().BeSameAs(failure);

        (await success.EnsureAsync<int>((Func<int, Task<bool>>?)null, guard, token))
            .Errors[0].Code.Should().Be("result.flow.callback_missing");

        (await success.EnsureAsync<int>(_ => null!, guard, token))
            .Errors[0].Code.Should().Be("result.flow.task_missing");

        (await success.EnsureAsync(v => Task.FromResult(v > 0), guard, token))
            .Should().BeSameAs(success);

        var rejected = await success.EnsureAsync(v => Task.FromResult(v < 0), guard, token);
        rejected.IsFailure.Should().BeTrue();
        rejected.Errors[0].Code.Should().Be("test.conflict");
        rejected.Metadata["source"].Should().Be("kept");
    }

    /// <summary>EnsureAsync token-aware overload should forward the token.</summary>
    [Fact]
    public async Task EnsureAsyncTokenAware_ShouldForwardToken()
    {
        var token = TestContext.Current.CancellationToken;
        var success = Result<int>.Ok(7);
        var guard = ResultStub.ConflictError();
        var received = CancellationToken.None;

        var ensured = await success.EnsureAsync(
            (v, ct) =>
            {
                received = ct;
                return Task.FromResult(v > 0);
            },
            guard,
            token);

        ensured.Should().BeSameAs(success);
        received.Should().Be(token);

        (await success.EnsureAsync<int>(
            (Func<int, CancellationToken, Task<bool>>?)null,
            guard,
            token)).Errors[0].Code.Should().Be("result.flow.callback_missing");
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

    #region TapAsync

    /// <summary>TapAsync legacy overload should invoke the side effect only on success.</summary>
    [Fact]
    public async Task TapAsyncLegacy_ShouldInvokeOnlyOnSuccess()
    {
        var token = TestContext.Current.CancellationToken;
        var seen = 0;
        var success = Result<int>.Ok(5);
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        (await success.TapAsync(v => Task.FromResult(seen = v), token))
            .Should().BeSameAs(success);
        seen.Should().Be(5);

        (await success.TapAsync<int>((Func<int, Task>?)null, token))
            .Should().BeSameAs(success);

        (await success.TapAsync<int>(_ => null!, token))
            .Should().BeSameAs(success);

        (await failure.TapAsync(v => Task.FromResult(seen = v), token))
            .Should().BeSameAs(failure);
        seen.Should().Be(5);
    }

    /// <summary>TapAsync token-aware overload should forward the token.</summary>
    [Fact]
    public async Task TapAsyncTokenAware_ShouldForwardToken()
    {
        var token = TestContext.Current.CancellationToken;
        var success = Result<int>.Ok(5);
        var received = CancellationToken.None;

        (await success.TapAsync(
            (v, ct) =>
            {
                received = ct;
                return Task.CompletedTask;
            },
            token)).Should().BeSameAs(success);
        received.Should().Be(token);

        (await success.TapAsync((_, _) => (Task)null!, token))
            .Should().BeSameAs(success);

        (await success.TapAsync<int>(
            (Func<int, CancellationToken, Task>?)null,
            token)).Should().BeSameAs(success);
    }

    #endregion

    #region TapErrorAsync

    /// <summary>TapErrorAsync legacy overload should invoke the side effect only on failure.</summary>
    [Fact]
    public async Task TapErrorAsyncLegacy_ShouldInvokeOnlyOnFailure()
    {
        var token = TestContext.Current.CancellationToken;
        List<Error>? seen = null;
        var success = Result<int>.Ok(1);
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        (await success.TapErrorAsync(errors => Task.FromResult(seen = errors), token))
            .Should().BeSameAs(success);
        seen.Should().BeNull();

        (await failure.TapErrorAsync(errors => Task.FromResult(seen = errors), token))
            .Should().BeSameAs(failure);
        seen.Should().NotBeNull();
        seen![0].Code.Should().Be("test.not_found");

        (await failure.TapErrorAsync<int>((Func<List<Error>, Task>?)null, token))
            .Should().BeSameAs(failure);

        (await failure.TapErrorAsync<int>(_ => null!, token))
            .Should().BeSameAs(failure);
    }

    /// <summary>TapErrorAsync token-aware overload should forward the token.</summary>
    [Fact]
    public async Task TapErrorAsyncTokenAware_ShouldForwardToken()
    {
        var token = TestContext.Current.CancellationToken;
        var failure = Result<int>.Fail(ResultStub.NotFoundError());
        var received = CancellationToken.None;

        (await failure.TapErrorAsync(
            (_, ct) =>
            {
                received = ct;
                return Task.CompletedTask;
            },
            token)).Should().BeSameAs(failure);
        received.Should().Be(token);

        (await failure.TapErrorAsync((_, _) => (Task)null!, token))
            .Should().BeSameAs(failure);

        (await failure.TapErrorAsync<int>(
            (Func<List<Error>, CancellationToken, Task>?)null,
            token)).Should().BeSameAs(failure);
    }

    #endregion

    #region SwitchAsync

    /// <summary>SwitchAsync legacy overload should select the side effect by state.</summary>
    [Fact]
    public async Task SwitchAsyncLegacy_ShouldSelectSideEffectByState()
    {
        var token = TestContext.Current.CancellationToken;
        var success = Result.Ok();
        var failure = Result.Fail(ResultStub.NotFoundError());
        var invoked = string.Empty;

        await success.SwitchAsync(
            () => Task.FromResult(invoked = "ok"),
            _ => Task.FromResult(invoked = "fail"),
            token);
        invoked.Should().Be("ok");

        await failure.SwitchAsync(
            () => Task.FromResult(invoked = "ok"),
            _ => Task.FromResult(invoked = "fail"),
            token);
        invoked.Should().Be("fail");

        await success.SwitchAsync((Func<Task>?)null, (Func<List<Error>, Task>?)null, token);
        await failure.SwitchAsync((Func<Task>?)null, (Func<List<Error>, Task>?)null, token);
    }

    /// <summary>SwitchAsync legacy overload should tolerate null tasks.</summary>
    [Fact]
    public async Task SwitchAsyncLegacy_ShouldTolerateNullTasks()
    {
        var token = TestContext.Current.CancellationToken;
        var success = Result.Ok();
        var failure = Result.Fail(ResultStub.NotFoundError());
        Func<Task> nullSuccessTask = () => null!;
        Func<List<Error>, Task> nullFailureTask = _ => null!;

        await success.SwitchAsync(nullSuccessTask, _ => Task.CompletedTask, token);
        await failure.SwitchAsync(() => Task.CompletedTask, nullFailureTask, token);
    }

    /// <summary>SwitchAsync token-aware overload should forward the token.</summary>
    [Fact]
    public async Task SwitchAsyncTokenAware_ShouldForwardToken()
    {
        var token = TestContext.Current.CancellationToken;
        var success = Result.Ok();
        var failure = Result.Fail(ResultStub.NotFoundError());
        var received = CancellationToken.None;
        var invoked = string.Empty;

        await success.SwitchAsync(
            ct =>
            {
                received = ct;
                return Task.FromResult(invoked = "ok");
            },
            (_, _) => Task.FromResult(invoked = "fail"),
            token);
        invoked.Should().Be("ok");
        received.Should().Be(token);

        await failure.SwitchAsync(
            _ => Task.FromResult(invoked = "ok"),
            (errors, ct) =>
            {
                received = ct;
                return Task.FromResult(invoked = errors[0].Code);
            },
            token);
        invoked.Should().Be("test.not_found");
        received.Should().Be(token);

        await success.SwitchAsync(
            (Func<CancellationToken, Task>?)null,
            (Func<List<Error>, CancellationToken, Task>?)null,
            token);

        await success.SwitchAsync(_ => null!, (_, _) => Task.CompletedTask, token);
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

    #region Cancellation

    /// <summary>A pre-cancelled token should cancel the flow before callbacks run.</summary>
    [Fact]
    public async Task PreCancelledToken_ShouldCancelFlowsBeforeCallbacksRun()
    {
        var cancelled = new CancellationToken(canceled: true);
        var called = false;
        var success = Result<int>.Ok(1);

        Func<Task<Result<bool>>> mapAct = () => success.MapAsync(
            v => Task.FromResult(called = true), cancelled);
        await mapAct.Should().ThrowAsync<OperationCanceledException>();

        Func<Task<bool>> matchAct = () => success.MatchAsync(
            v => Task.FromResult(called = true),
            _ => Task.FromResult(false),
            cancelled);
        await matchAct.Should().ThrowAsync<OperationCanceledException>();

        Func<Task<Result<int>>> ensureAct = () => success.EnsureAsync(
            v => Task.FromResult(called = true),
            ResultStub.ConflictError(),
            cancelled);
        await ensureAct.Should().ThrowAsync<OperationCanceledException>();

        Func<Task<Result<int>>> tryAct = () => ((Func<Task<int>>)(() => Task.FromResult(1)))
            .TryAsync(null, cancelled);
        await tryAct.Should().ThrowAsync<OperationCanceledException>();

        called.Should().BeFalse();
    }

    #endregion
}