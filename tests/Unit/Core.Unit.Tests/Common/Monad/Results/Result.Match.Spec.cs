namespace Core.Unit.Tests.Common.Monad.Results;

/// <summary>Tests for Match and Fold extensions in <see cref="ResultExtension"/>.</summary>
[Trait("Category", "Unit")]
public class ResultMatchSpec
{
    #region Pattern Matching

    /// <summary>Match should select the callback by state and default when the callback is missing.</summary>
    [Fact]
    public void Match_ShouldSelectCallbackByStateAndDefault_WhenMissing()
    {
        // Arrange
        var success = Result.Ok();
        var failure = Result.Fail(ResultStub.NotFoundError());

        // Act & Assert
        success.Match(() => "ok", _ => "fail").Should().Be("ok");
        failure.Match(() => "ok", errors => errors[0].Code).Should().Be("test.not_found");
        success.Match<int?>(null, _ => 1).Should().BeNull();
        failure.Match<int?>(() => 1, null).Should().BeNull();
    }

    /// <summary>Fold should behave like Match.</summary>
    [Fact]
    public void Fold_ShouldBehaveLikeMatch()
    {
        // Arrange
        var success = Result.Ok();
        var failure = Result.Fail(ResultStub.NotFoundError());

        // Act & Assert
        success.Fold(() => 1, _ => 2).Should().Be(1);
        failure.Fold(() => 1, _ => 2).Should().Be(2);
    }

    #endregion

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
}
