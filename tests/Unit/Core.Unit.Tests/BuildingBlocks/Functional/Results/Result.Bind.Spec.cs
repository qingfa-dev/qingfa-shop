using System.Reflection;
using System.Runtime.CompilerServices;

namespace Core.Unit.Tests.BuildingBlocks.Functional.Results;

/// <summary>Tests for Bind and Flatten extensions in <see cref="ResultExtension"/>.</summary>
[Trait("Category", "Unit")]
public class ResultBindSpec
{
    #region Bind

    /// <summary>Non-generic Bind should propagate failure, report missing callbacks and bind success.</summary>
    [Fact]
    public void Bind_ShouldPropagateFailureReportMissingAndBindSuccess()
    {
        // Arrange
        var failure = Result.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");
        var success = Result.Ok()
            .WithMetadata("source", "kept");

        // Act & Assert
        var propagated = failure.Bind(() => Result<int>.Ok(1));
        propagated.IsFailure.Should().BeTrue();
        propagated.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        propagated.Metadata["source"].Should().Be("kept");

        var missing = success.Bind((Func<Result<int>>?)null);
        missing.Errors[0].Code.Should().Be("result.flow.callback_missing");

        var nullBound = success.Bind<int>(() => null!);
        nullBound.Errors[0].Code.Should().Be("result.flow.result_missing");

        var bound = success.Bind(() => Result<int>.Ok(9));
        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be(9);
        bound.Metadata["source"].Should().Be("kept");
    }

    /// <summary>Generic Bind should propagate failure, report missing callbacks and bind success.</summary>
    [Fact]
    public void GenericBind_ShouldPropagateFailureReportMissingAndBindSuccess()
    {
        // Arrange
        var failure = Result<int>.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");
        var success = Result<int>.Ok(4)
            .WithMetadata("source", "kept");

        // Act & Assert
        var propagated = failure.Bind(v => Result<string>.Ok(v.ToString()));
        propagated.IsFailure.Should().BeTrue();
        propagated.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        propagated.Metadata["source"].Should().Be("kept");

        var missing = success.Bind((Func<int, Result<string>>?)null);
        missing.Errors[0].Code.Should().Be("result.flow.callback_missing");

        var nullBound = success.Bind<int, string>(_ => null!);
        nullBound.Errors[0].Code.Should().Be("result.flow.result_missing");

        var bound = success.Bind(v => Result<string>.Ok($"value-{v}"));
        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("value-4");
        bound.Metadata["source"].Should().Be("kept");
    }

    #endregion

    #region Flatten

    /// <summary>Flatten should unwrap success and propagate failure.</summary>
    [Fact]
    public void Flatten_ShouldUnwrapSuccessAndPropagateFailure()
    {
        // Arrange
        var failure = Result<Result<int>>.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");
        var success = Result<Result<int>>.Ok(Result<int>.Ok(5))
            .WithMetadata("source", "kept");

        // Act & Assert
        var propagated = failure.Flatten();
        propagated.IsFailure.Should().BeTrue();
        propagated.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        propagated.Metadata["source"].Should().Be("kept");

        var flattened = success.Flatten();
        flattened.IsSuccess.Should().BeTrue();
        flattened.Value.Should().Be(5);
        flattened.Metadata["source"].Should().Be("kept");
    }

    /// <summary>Flatten should reject a success holder with a null inner result.</summary>
    [Fact]
    public void FlattenWithNullInnerResult_ShouldFailResultMissing()
    {
        // Arrange — bypass factory validation to simulate a corrupted holder
        var holder = (Result<Result<int>>)RuntimeHelpers.GetUninitializedObject(
            typeof(Result<Result<int>>));
        typeof(Result)
            .GetField(
                "<IsSuccess>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(holder, true);
        typeof(Result<Result<int>>)
            .GetField(
                "<Value>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(holder, null);

        // Act
        var flattened = holder.Flatten();

        // Assert
        flattened.IsFailure.Should().BeTrue();
        flattened.Errors[0].Code.Should().Be("result.flow.result_missing");
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
}
