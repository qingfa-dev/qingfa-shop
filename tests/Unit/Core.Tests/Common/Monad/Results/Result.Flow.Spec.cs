using System.Reflection;
using System.Runtime.CompilerServices;

namespace Core.UnitTests.Common.Monad.Results;

/// <summary>Tests for the synchronous flow extensions in <see cref="ResultExtension"/>.</summary>
[Trait("Category", "Unit")]
public class ResultFlowSpec
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

    #region Map

    /// <summary>Non-generic Map should propagate failure, report missing callbacks and map success.</summary>
    [Fact]
    public void Map_ShouldPropagateFailureReportMissingCallbackAndMapSuccess()
    {
        // Arrange
        var failure = Result.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");
        var success = Result.Ok()
            .WithMetadata("source", "kept");

        // Act & Assert
        var propagated = failure.Map(() => 42);
        propagated.IsFailure.Should().BeTrue();
        propagated.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        propagated.Metadata["source"].Should().Be("kept");

        var missing = success.Map((Func<int>?)null);
        missing.IsFailure.Should().BeTrue();
        missing.Errors[0].Code.Should().Be("result.flow.callback_missing");

        var mapped = success.Map(() => 42);
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(42);
        mapped.Metadata["source"].Should().Be("kept");
    }

    /// <summary>Generic Map should propagate failure, report missing callbacks and map success.</summary>
    [Fact]
    public void GenericMap_ShouldPropagateFailureReportMissingCallbackAndMapValue()
    {
        // Arrange
        var failure = Result<int>.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");
        var success = Result<int>.Ok(7)
            .WithMetadata("source", "kept");

        // Act & Assert
        var propagated = failure.Map(v => v + 1);
        propagated.IsFailure.Should().BeTrue();
        propagated.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        propagated.Metadata["source"].Should().Be("kept");

        var missing = success.Map((Func<int, int>?)null);
        missing.IsFailure.Should().BeTrue();
        missing.Errors[0].Code.Should().Be("result.flow.callback_missing");

        var mapped = success.Map(v => v + 1);
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(8);
        mapped.Metadata["source"].Should().Be("kept");
    }

    #endregion

    #region MapError

    /// <summary>Non-generic MapError should pass through success and map each failure error.</summary>
    [Fact]
    public void MapError_ShouldPassSuccessMapFailuresAndReportMissingCallbackOrError()
    {
        // Arrange
        var success = Result.Ok();
        var failure = Result.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");

        // Act & Assert
        failure.MapError(null).Errors[0].Code.Should().Be("result.flow.callback_missing");

        success.MapError(null).Should().BeSameAs(success);

        var mapped = failure.MapError(
            error => Error.Custom(error.Code, error.Message));
        mapped.IsFailure.Should().BeTrue();
        mapped.Errors[0].Code.Should().Be("test.not_found");
        mapped.Metadata["source"].Should().Be("kept");

        var nullMapped = failure.MapError(_ => null!);
        nullMapped.Errors[0].Code.Should().Be("result.flow.error_missing");
    }

    /// <summary>Generic MapError should pass through success and map each failure error.</summary>
    [Fact]
    public void GenericMapError_ShouldPassSuccessMapFailuresAndReportMissingCallbackOrError()
    {
        // Arrange
        var success = Result<int>.Ok(3);
        var failure = Result<int>.Fail(ResultStub.NotFoundError())
            .WithMetadata("source", "kept");

        // Act & Assert
        failure.MapError(null).Errors[0].Code.Should().Be("result.flow.callback_missing");

        success.MapError(null).Should().BeSameAs(success);

        var mapped = failure.MapError(
            error => Error.Custom(error.Code, error.Message));
        mapped.IsFailure.Should().BeTrue();
        mapped.Errors[0].Code.Should().Be("test.not_found");
        mapped.Metadata["source"].Should().Be("kept");

        var nullMapped = failure.MapError(_ => null!);
        nullMapped.Errors[0].Code.Should().Be("result.flow.error_missing");
    }

    #endregion

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

    #region Combination

    /// <summary>Combine should succeed for empty and all-success input.</summary>
    [Fact]
    public void Combine_ShouldSucceedForEmptyAndAllSuccessInput()
    {
        // Act & Assert
        ResultExtension.Combine().IsSuccess.Should().BeTrue();
        ResultExtension.Combine(Result.Ok(), Result<int>.Ok(1)).IsSuccess.Should().BeTrue();
    }

    /// <summary>Combine should report a null element.</summary>
    [Fact]
    public void CombineWithNullElement_ShouldReportResultMissing()
    {
        // Act
        var combined = ResultExtension.Combine((Result)null!);

        // Assert
        combined.IsFailure.Should().BeTrue();
        combined.Errors[0].Code.Should().Be("result.flow.result_missing");
    }

    /// <summary>Combine should merge same-status failures.</summary>
    [Fact]
    public void CombineWithSameStatusFailures_ShouldMergeErrors()
    {
        // Arrange
        var first = Result.Fail(ResultStub.NotFoundError());
        var second = Result.Fail(ResultStub.NotFoundError());

        // Act
        var combined = ResultExtension.Combine(first, second);

        // Assert
        combined.IsFailure.Should().BeTrue();
        combined.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        combined.Errors.Count.Should().Be(2);
    }

    /// <summary>Combine should report mixed failure statuses.</summary>
    [Fact]
    public void CombineWithMixedStatusFailures_ShouldReportMixed()
    {
        // Arrange
        var first = Result.Fail(ResultStub.NotFoundError());
        var second = Result.Fail(ResultStub.ConflictError());

        // Act
        var combined = ResultExtension.Combine(first, second);

        // Assert
        combined.IsFailure.Should().BeTrue();
        combined.Errors[0].Code.Should().Be("result.errors.mixed_status_codes");
    }

    #endregion

    #region Extraction

    /// <summary>ValueOr should return the value or the fallback.</summary>
    [Fact]
    public void ValueOr_ShouldReturnValueOrFallback()
    {
        // Arrange
        var success = Result<int>.Ok(5);
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        // Act & Assert
        success.ValueOr(9).Should().Be(5);
        failure.ValueOr(9).Should().Be(9);
        failure.ValueOr().Should().Be(default);
    }

    /// <summary>ValueOrThrow should return the value or throw a ResultException.</summary>
    [Fact]
    public void ValueOrThrow_ShouldReturnValueOrThrow()
    {
        // Arrange
        var success = Result<int>.Ok(5);
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        // Act & Assert
        success.ValueOrThrow().Should().Be(5);

        Action act = () =>
        {
            _ = failure.ValueOrThrow();
        };

        var exception = act.Should().Throw<ResultException>();
        exception.Which.Errors[0].Code.Should().Be("test.not_found");
        exception.Which.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
    }

    /// <summary>ThrowIfFailure should pass successes and throw on failures.</summary>
    [Fact]
    public void ThrowIfFailure_ShouldPassSuccessAndThrowOnFailure()
    {
        // Arrange
        var success = Result.Ok();
        var failure = Result.Fail(ResultStub.NotFoundError());
        var genericSuccess = Result<int>.Ok(1);
        var genericFailure = Result<int>.Fail(ResultStub.NotFoundError());

        // Act & Assert
        success.ThrowIfFailure().Should().BeSameAs(success);
        genericSuccess.ThrowIfFailure().Should().BeSameAs(genericSuccess);

        Action failureAction = () => failure.ThrowIfFailure();
        failureAction.Should().Throw<ResultException>();

        Action genericFailureAction = () => genericFailure.ThrowIfFailure();
        genericFailureAction.Should().Throw<ResultException>();
    }

    #endregion
}