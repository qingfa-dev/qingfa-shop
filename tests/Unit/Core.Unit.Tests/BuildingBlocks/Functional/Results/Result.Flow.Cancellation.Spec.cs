namespace Core.Unit.Tests.BuildingBlocks.Functional.Results;

/// <summary>Tests for cancellation behavior in asynchronous result extensions.</summary>
[Trait("Category", "Unit")]
public class ResultFlowCancellationSpec
{
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
