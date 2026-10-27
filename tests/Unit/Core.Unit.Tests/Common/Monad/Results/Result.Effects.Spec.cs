namespace Core.Unit.Tests.Common.Monad.Results;

/// <summary>Tests for Tap, TapError, and Switch extensions in <see cref="ResultExtension"/>.</summary>
[Trait("Category", "Unit")]
public class ResultEffectsSpec
{
    #region Effects

    /// <summary>Tap should invoke the action only on success.</summary>
    [Fact]
    public void Tap_ShouldInvokeOnlyOnSuccess()
    {
        // Arrange
        var invoked = 0;
        var success = Result.Ok();
        var failure = Result.Fail(ResultStub.NotFoundError());

        // Act & Assert
        success.Tap(() => invoked++).Should().BeSameAs(success);
        success.Tap((Action?)null).Should().BeSameAs(success);
        failure.Tap(() => invoked++).Should().BeSameAs(failure);
        invoked.Should().Be(1);
    }

    /// <summary>Generic Tap should invoke the action only on success.</summary>
    [Fact]
    public void GenericTap_ShouldInvokeOnlyOnSuccess()
    {
        // Arrange
        var seen = 0;
        var success = Result<int>.Ok(5);
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        // Act & Assert
        success.Tap(v => seen = v).Should().BeSameAs(success);
        success.Tap((Action<int>?)null).Should().BeSameAs(success);
        failure.Tap(v => seen = v).Should().BeSameAs(failure);
        seen.Should().Be(5);
    }

    /// <summary>TapError should invoke the action only on failure.</summary>
    [Fact]
    public void TapError_ShouldInvokeOnlyOnFailure()
    {
        // Arrange
        List<Error>? seen = null;
        var success = Result.Ok();
        var failure = Result.Fail(ResultStub.NotFoundError());

        // Act & Assert
        success.TapError(errors => seen = errors).Should().BeSameAs(success);
        failure.TapError(errors => seen = errors).Should().BeSameAs(failure);
        failure.TapError((Action<List<Error>>?)null).Should().BeSameAs(failure);
        seen.Should().NotBeNull();
        seen[0].Code.Should().Be("test.not_found");
    }

    /// <summary>Generic TapError should invoke the action only on failure.</summary>
    [Fact]
    public void GenericTapError_ShouldInvokeOnlyOnFailure()
    {
        // Arrange
        List<Error>? seen = null;
        var success = Result<int>.Ok(1);
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        // Act & Assert
        success.TapError(errors => seen = errors).Should().BeSameAs(success);
        failure.TapError(errors => seen = errors).Should().BeSameAs(failure);
        failure.TapError((Action<List<Error>>?)null).Should().BeSameAs(failure);
        seen.Should().NotBeNull();
        seen[0].Code.Should().Be("test.not_found");
    }

    /// <summary>Switch should select the side effect by state.</summary>
    [Fact]
    public void Switch_ShouldSelectSideEffectByState()
    {
        // Arrange
        var success = Result.Ok();
        var failure = Result.Fail(ResultStub.NotFoundError());
        var invoked = string.Empty;

        // Act & Assert
        success.Switch(() => invoked = "ok", _ => invoked = "fail")
            .Should().BeSameAs(success);
        invoked.Should().Be("ok");

        failure.Switch(() => invoked = "ok", _ => invoked = "fail")
            .Should().BeSameAs(failure);
        invoked.Should().Be("fail");

        success.Switch(null, null).Should().BeSameAs(success);
        failure.Switch(null, null).Should().BeSameAs(failure);
    }

    /// <summary>Generic Switch should select the side effect by state.</summary>
    [Fact]
    public void GenericSwitch_ShouldSelectSideEffectByState()
    {
        // Arrange
        var success = Result<int>.Ok(5);
        var failure = Result<int>.Fail(ResultStub.NotFoundError());
        var invoked = string.Empty;

        // Act & Assert
        success.Switch(v => invoked = $"ok:{v}", _ => invoked = "fail")
            .Should().BeSameAs(success);
        invoked.Should().Be("ok:5");

        failure.Switch(v => invoked = $"ok:{v}", _ => invoked = "fail")
            .Should().BeSameAs(failure);
        invoked.Should().Be("fail");

        success.Switch(null, null).Should().BeSameAs(success);
        failure.Switch(null, null).Should().BeSameAs(failure);
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
}
