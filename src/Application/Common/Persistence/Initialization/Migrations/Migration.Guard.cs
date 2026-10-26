namespace Application.Common.Persistence.Initialization.Migrations;

using Core.Common.Monad.Results;

/// <summary>
/// Guard utilities for validating migration-related values against <see cref="MigrationConstant"/> limits.
/// </summary>
/// <remarks>
/// Returns <see cref="Result"/> instead of throwing, enabling monadic validation composition.
/// </remarks>
public static class MigrationGuard
{
    #region Migration Name

    /// <summary>
    /// Validates a migration name.
    /// </summary>
    /// <param name="name">The migration name to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateName(
        string? name,
        string parameterName = "name")
    {
        if (name is null)
        {
            return Result.Fail(MigrationOutcome.Failure.NameNull(parameterName));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail(MigrationOutcome.Failure.NameNullOrWhitespace(parameterName));
        }

        if (name.Length > MigrationConstant.Limits.MaxNameLength)
        {
            return Result.Fail(MigrationOutcome.Failure.NameExceedsMaxLength(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates a migration order.
    /// </summary>
    /// <param name="order">The migration order to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateOrder(
        int order,
        string parameterName = "order")
    {
        if (order < 0)
        {
            return Result.Fail(MigrationOutcome.Failure.OrderNegative(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates the number of registered migrations.
    /// </summary>
    /// <param name="count">The number of migrations.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateCount(
        int count,
        string parameterName = "count")
    {
        if (count < 0)
        {
            return Result.Fail(MigrationOutcome.Failure.CountNegative(parameterName));
        }

        if (count > MigrationConstant.Limits.MaxMigrations)
        {
            return Result.Fail(MigrationOutcome.Failure.CountExceedsMax(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates a migration version format.
    /// </summary>
    /// <param name="version">The version string to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateVersion(
        string? version,
        string parameterName = "version")
    {
        if (version is null)
        {
            return Result.Fail(MigrationOutcome.Failure.VersionNull(parameterName));
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            return Result.Fail(MigrationOutcome.Failure.VersionNullOrWhitespace(parameterName));
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d{14}$"))
        {
            return Result.Fail(MigrationOutcome.Failure.VersionInvalidFormat(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates a migration checksum.
    /// </summary>
    /// <param name="checksum">The checksum to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateChecksum(
        string? checksum,
        string parameterName = "checksum")
    {
        if (checksum is null)
        {
            return Result.Fail(MigrationOutcome.Failure.ChecksumNull(parameterName));
        }

        if (string.IsNullOrWhiteSpace(checksum))
        {
            return Result.Fail(MigrationOutcome.Failure.ChecksumNullOrWhitespace(parameterName));
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(checksum, @"^[a-fA-F0-9]{64}$"))
        {
            return Result.Fail(MigrationOutcome.Failure.ChecksumInvalidFormat(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates all migration properties at once.
    /// </summary>
    /// <param name="name">The migration name.</param>
    /// <param name="order">The migration order.</param>
    /// <param name="version">The migration version.</param>
    /// <param name="checksum">The migration checksum.</param>
    /// <returns>A successful Result if all valid; otherwise a failed Result with combined errors.</returns>
    public static Result ValidateMigration(
        string? name,
        int order,
        string? version,
        string? checksum)
    {
        var results = new List<Result>
        {
            ValidateName(name),
            ValidateOrder(order),
            ValidateVersion(version)
        };

        if (checksum is not null)
        {
            results.Add(ValidateChecksum(checksum));
        }

        return ResultExtension.Combine([.. results]);
    }

    #endregion
}