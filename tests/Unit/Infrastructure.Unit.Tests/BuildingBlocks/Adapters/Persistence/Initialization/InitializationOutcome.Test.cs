using Application.BuildingBlocks.Persistence.Initialization;
using Application.BuildingBlocks.Persistence.Initialization.Migrations;
using Application.BuildingBlocks.Persistence.Initialization.Seeders;
using Application.BuildingBlocks.Persistence.Initialization.Resetters;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.Initialization;

/// <summary>Unit tests for InitializationOutcome.</summary>
public sealed class InitializationOutcomeTest
{
    [Fact]
    public void MigratorsMissing_ShouldHaveCorrectCode()
    {
        var error = InitializationOutcome.Failure.MigratorsMissing;
        error.Code.Should().Be(InitializationOutcome.Codes.MigratorsMissing);
        error.Message.Should().Contain("migrators");
    }

    [Fact]
    public void SchemaResetterMissing_ShouldHaveCorrectCode()
    {
        var error = InitializationOutcome.Failure.SchemaResetterMissing;
        error.Code.Should().Be(InitializationOutcome.Codes.SchemaResetterMissing);
        error.Message.Should().Contain("resetter");
    }

    [Fact]
    public void SchemaResetRejected_ShouldIncludeReason()
    {
        var error = InitializationOutcome.Failure.SchemaResetRejected("test reason");
        error.Code.Should().Be(InitializationOutcome.Codes.SchemaResetRejected);
        error.Message.Should().Contain("test reason");
    }

    [Fact]
    public void MigrationFailed_ShouldIncludeName()
    {
        var error = InitializationOutcome.Failure.MigrationFailed("test-migrator");
        error.Code.Should().Be(InitializationOutcome.Codes.MigrationFailed);
        error.Message.Should().Contain("test-migrator");
    }

    [Fact]
    public void SeedingFailed_ShouldIncludeCounts()
    {
        var error = InitializationOutcome.Failure.SeedingFailed(3, 1);
        error.Code.Should().Be(InitializationOutcome.Codes.SeedingFailed);
        error.Message.Should().Contain("3");
        error.Message.Should().Contain("1");
    }

    [Fact]
    public void Unexpected_ShouldIncludeReason()
    {
        var error = InitializationOutcome.Failure.Unexpected("something went wrong");
        error.Code.Should().Be(InitializationOutcome.Codes.Unexpected);
        error.Message.Should().Contain("something went wrong");
    }

    [Fact]
    public void NoStepsRegistered_ShouldHaveCorrectCode()
    {
        var error = InitializationOutcome.Failure.NoStepsRegistered;
        error.Code.Should().Be(InitializationOutcome.Codes.NoStepsRegistered);
    }

    [Fact]
    public void DuplicateStepName_ShouldIncludeName()
    {
        var error = InitializationOutcome.Failure.DuplicateStepName("step1");
        error.Code.Should().Be(InitializationOutcome.Codes.DuplicateStepName);
        error.Message.Should().Contain("step1");
    }

    [Fact]
    public void MissingDependency_ShouldIncludeNames()
    {
        var error = InitializationOutcome.Failure.MissingDependency("step1", "step2");
        error.Code.Should().Be(InitializationOutcome.Codes.MissingDependency);
        error.Message.Should().Contain("step1");
        error.Message.Should().Contain("step2");
    }

    [Fact]
    public void StepFailed_ShouldIncludeNameAndErrors()
    {
        var errors = new List<Error> { Error.Conflict("code", "message") };
        var error = InitializationOutcome.Failure.StepFailed("step1", errors);
        error.Code.Should().Be(InitializationOutcome.Codes.StepFailed);
        error.Message.Should().Contain("step1");
    }

    [Fact]
    public void CircularDependency_ShouldIncludeName()
    {
        var error = InitializationOutcome.Failure.CircularDependency("step1");
        error.Code.Should().Be(InitializationOutcome.Codes.CircularDependency);
        error.Message.Should().Contain("step1");
    }
}

/// <summary>Unit tests for MigrationOutcome.</summary>
public sealed class MigrationOutcomeTest
{
    [Fact]
    public void MigrationFailed_ShouldIncludeName()
    {
        var error = MigrationOutcome.Failure.MigrationFailed("test");
        error.Message.Should().Contain("test");
    }

    [Fact]
    public void StoreUnavailable_ShouldIncludeName()
    {
        var error = MigrationOutcome.Failure.StoreUnavailable("test");
        error.Message.Should().Contain("test");
    }

    [Fact]
    public void Timeout_ShouldIncludeName()
    {
        var error = MigrationOutcome.Failure.Timeout("test");
        error.Message.Should().Contain("test");
    }

    [Fact]
    public void Cancelled_ShouldIncludeName()
    {
        var error = MigrationOutcome.Failure.Cancelled("test");
        error.Message.Should().Contain("test");
    }
}

/// <summary>Unit tests for SeederOutcome.</summary>
public sealed class SeederOutcomeTest
{
    [Fact]
    public void OrderNegative_ShouldHaveCorrectCode()
    {
        var error = SeederOutcome.Failure.OrderNegative();
        error.Code.Should().Be(SeederOutcome.Codes.OrderNegative);
    }

    [Fact]
    public void CountNegative_ShouldHaveCorrectCode()
    {
        var error = SeederOutcome.Failure.CountNegative();
        error.Code.Should().Be(SeederOutcome.Codes.CountNegative);
    }

    [Fact]
    public void CountExceedsMax_ShouldIncludeLimit()
    {
        var error = SeederOutcome.Failure.CountExceedsMax();
        error.Message.Should().Contain(SeederConstant.Limits.MaxSeeders.ToString());
    }

    [Fact]
    public void BatchSizeTooSmall_ShouldIncludeLimit()
    {
        var error = SeederOutcome.Failure.BatchSizeTooSmall();
        error.Message.Should().Contain(SeederConstant.Limits.MinBatchSize.ToString());
    }

    [Fact]
    public void BatchSizeTooLarge_ShouldIncludeLimit()
    {
        var error = SeederOutcome.Failure.BatchSizeTooLarge();
        error.Message.Should().Contain(SeederConstant.Limits.MaxBatchSize.ToString());
    }
}

/// <summary>Unit tests for ResetterOutcome.</summary>
public sealed class ResetterOutcomeTest
{
    [Fact]
    public void OrderNegative_ShouldHaveCorrectCode()
    {
        var error = ResetterOutcome.Failure.OrderNegative();
        error.Code.Should().Be(ResetterOutcome.Codes.OrderNegative);
    }

    [Fact]
    public void CountExceedsMax_ShouldIncludeLimit()
    {
        var error = ResetterOutcome.Failure.CountExceedsMax();
        error.Message.Should().Contain(ResetterConstant.Limits.MaxResetters.ToString());
    }

    [Fact]
    public void ScopeNull_ShouldHaveCorrectCode()
    {
        var error = ResetterOutcome.Failure.ScopeNull();
        error.Code.Should().Be(ResetterOutcome.Codes.ScopeNull);
    }

    [Fact]
    public void ScopeInvalid_ShouldIncludeValidValues()
    {
        var error = ResetterOutcome.Failure.ScopeInvalid();
        error.Message.Should().Contain(ResetterConstant.Scope.Data);
        error.Message.Should().Contain(ResetterConstant.Scope.Schema);
        error.Message.Should().Contain(ResetterConstant.Scope.All);
    }
}

/// <summary>Unit tests for SchemaResetOutcome.</summary>
public sealed class SchemaResetOutcomeTest
{
    [Fact]
    public void ResetFailed_ShouldIncludeReason()
    {
        var error = SchemaResetOutcome.Failure.ResetFailed("test reason");
        error.Message.Should().Contain("test reason");
    }

    [Fact]
    public void StoreUnavailable_ShouldHaveCorrectCode()
    {
        var error = SchemaResetOutcome.Failure.StoreUnavailable;
        error.Code.Should().Be("Database.SchemaReset.StoreUnavailable");
    }

    [Fact]
    public void Timeout_ShouldHaveCorrectCode()
    {
        var error = SchemaResetOutcome.Failure.Timeout;
        error.Code.Should().Be("Database.SchemaReset.Timeout");
    }

    [Fact]
    public void Cancelled_ShouldHaveCorrectCode()
    {
        var error = SchemaResetOutcome.Failure.Cancelled;
        error.Code.Should().Be("Database.SchemaReset.Cancelled");
    }
}
