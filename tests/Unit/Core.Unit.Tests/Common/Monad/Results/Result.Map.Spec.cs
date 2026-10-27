using Core.Unit.Tests.Common.Monad.Results;

namespace Core.Unit.Tests.Common.Monad.Results;

/// <summary>Tests for Map and MapError extensions in <see cref="ResultExtension"/>.</summary>
[Trait("Category", "Unit")]
public class ResultMapSpec
{
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
}
