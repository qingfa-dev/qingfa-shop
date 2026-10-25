namespace Core.Tests.Common.Monad.Results;

/// <summary>Tests for Ensure extensions in <see cref="ResultExtension"/>.</summary>
[Trait("Category", "Unit")]
public class ResultEnsureSpec
{
    #region Ensure

    /// <summary>Non-generic Ensure should pass failures and enforce the predicate on success.</summary>
    [Fact]
    public void Ensure_ShouldPassFailuresAndEnforcePredicate()
    {
        // Arrange
        var failure = Result.Fail(ResultStub.NotFoundError());
        var success = Result.Ok()
            .WithMetadata("source", "kept");
        var guard = ResultStub.ConflictError();

        // Act & Assert
        failure.Ensure(null, null).Should().BeSameAs(failure);

        success.Ensure(null, guard).Errors[0].Code.Should().Be("result.flow.callback_missing");
        success.Ensure(() => true, null).Errors[0].Code.Should().Be("result.flow.error_missing");

        success.Ensure(() => true, guard).Should().BeSameAs(success);

        var rejected = success.Ensure(() => false, guard);
        rejected.IsFailure.Should().BeTrue();
        rejected.Errors[0].Code.Should().Be("test.conflict");
        rejected.Metadata["source"].Should().Be("kept");
    }

    /// <summary>Generic Ensure should pass failures and enforce the predicate on success.</summary>
    [Fact]
    public void GenericEnsure_ShouldPassFailuresAndEnforcePredicate()
    {
        // Arrange
        var failure = Result<int>.Fail(ResultStub.NotFoundError());
        var success = Result<int>.Ok(7)
            .WithMetadata("source", "kept");
        var guard = ResultStub.ConflictError();

        // Act & Assert
        failure.Ensure(null, null).Should().BeSameAs(failure);

        success.Ensure(null, guard).Errors[0].Code.Should().Be("result.flow.callback_missing");
        success.Ensure(_ => true, null).Errors[0].Code.Should().Be("result.flow.error_missing");

        success.Ensure(v => v > 0, guard).Should().BeSameAs(success);

        var rejected = success.Ensure(v => v < 0, guard);
        rejected.IsFailure.Should().BeTrue();
        rejected.Errors[0].Code.Should().Be("test.conflict");
        rejected.Metadata["source"].Should().Be("kept");
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
}
