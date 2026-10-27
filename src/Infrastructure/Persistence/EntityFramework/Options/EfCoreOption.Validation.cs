using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Infrastructure.Persistence.EntityFramework.Options;

/// <summary>Validates persistence settings and returns the monad's <see cref="Result{TValue}"/>.</summary>
/// <remarks>
/// The composition uses this method to fail fast with inline validation.
/// </remarks>
public static class PersistenceSettingValidation
{
    /// <summary>Validates the persistence configuration (static rules only, without connection string resolution).</summary>
    /// <param name="setting">The settings to validate.</param>
    /// <returns>
    /// The unchanged settings on success; otherwise a failure carrying every broken rule.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <c>null</c>.</exception>
    public static Result<PersistenceSetting> Validate(PersistenceSetting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);

        // Compute: validate each rule independently and collect all errors.
        Result combined = ResultExtension.Combine(
            ValidateConnectionStringName(setting),
            ValidateProvider(setting),
            ValidateCommandTimeout(setting),
            ValidateRetryPolicy(setting),
            ValidatePooling(setting));

        // Return: preserve every error the combined result collected.
        return combined.IsSuccess
            ? Result<PersistenceSetting>.Success(setting)
            : Result<PersistenceSetting>.Fail(combined.Errors, combined.StatusCode);
    }

    /// <summary>
    /// Validates the persistence configuration including connection string resolution from IConfiguration.
    /// </summary>
    /// <param name="setting">The settings to validate.</param>
    /// <param name="configuration">The configuration to resolve the connection string from.</param>
    /// <returns>
    /// The unchanged settings on success; otherwise a failure carrying every broken rule.
    /// </returns>
    public static Result<PersistenceSetting> ValidateWithConnectionString(
        PersistenceSetting setting, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // First validate static rules
        Result<PersistenceSetting> staticResult = Validate(setting);
        if (staticResult.IsFailure)
        {
            return staticResult;
        }

        // Resolve and validate the connection string
        Result connectionStringResult = ValidateAndResolveConnectionString(setting, configuration);
        if (connectionStringResult.IsFailure)
        {
            return Result<PersistenceSetting>.Fail(connectionStringResult.Errors, connectionStringResult.StatusCode);
        }

        return Result<PersistenceSetting>.Success(setting);
    }

    /// <summary>
    /// Validates the persistence configuration and probes the database connection.
    /// Use this at startup to verify the connection string actually works.
    /// </summary>
    /// <param name="setting">The settings to validate.</param>
    /// <param name="configuration">The configuration to resolve the connection string from.</param>
    /// <param name="cancellationToken">Cancellation token for the probe.</param>
    /// <returns>
    /// The unchanged settings on success; otherwise a failure carrying every broken rule.
    /// </returns>
    public static async Task<Result<PersistenceSetting>> ValidateWithConnectionProbeAsync(
        PersistenceSetting setting, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // First validate with connection string resolution
        Result<PersistenceSetting> validationResult = ValidateWithConnectionString(setting, configuration);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        // Resolve the connection string for probing
        string? connectionString = configuration.GetConnectionString(setting.ConnectionStringName!);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Result<PersistenceSetting>.Fail(
                PersistenceSettingOutcome.Failure.ConnectionStringNotFound(setting.ConnectionStringName!));
        }

        // Then probe the database connection
        Result probeResult = await ProbeConnectionAsync(connectionString, cancellationToken);
        if (probeResult.IsFailure)
        {
            return Result<PersistenceSetting>.Fail(probeResult.Errors, probeResult.StatusCode);
        }

        return Result<PersistenceSetting>.Success(setting);
    }

    /// <summary>Probes the database connection to verify the connection string works.</summary>
    private static async Task<Result> ProbeConnectionAsync(
        string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return Result.Success();
        }
        catch (NpgsqlException ex)
        {
            return Result.Fail(PersistenceSettingOutcome.Failure.ConnectionProbeFailed(ex.Message));
        }
        catch (OperationCanceledException)
        {
            return Result.Fail(PersistenceSettingOutcome.Failure.ConnectionProbeFailed("Connection probe timed out."));
        }
        catch (Exception ex)
        {
            return Result.Fail(PersistenceSettingOutcome.Failure.ConnectionProbeFailed(ex.Message));
        }
    }

    /// <summary>Formats a failed result as the messages the options pipeline reports.</summary>
    /// <param name="result">The result to describe.</param>
    /// <returns>One <c>Code: Message</c> entry per error.</returns>
    public static IEnumerable<string> Describe(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Errors.Select(error => $"{error.Code}: {error.Message}");
    }

    private static Result ValidateConnectionStringName(PersistenceSetting setting)
    {
        // Validate: connection string name is mandatory for a live adapter.
        if (string.IsNullOrWhiteSpace(setting.ConnectionStringName))
        {
            return Result.Fail(PersistenceSettingOutcome.Failure.ConnectionStringNameRequired);
        }

        return Result.Success();
    }

    private static Result ValidateAndResolveConnectionString(
        PersistenceSetting setting, IConfiguration configuration)
    {
        // Resolve: get the connection string from the ConnectionStrings section.
        string? connectionString = configuration.GetConnectionString(setting.ConnectionStringName!);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Result.Fail(PersistenceSettingOutcome.Failure.ConnectionStringNotFound(setting.ConnectionStringName!));
        }

        // Validate: connection string must contain at least a Host key for PostgreSQL
        if (setting.Provider.ToLowerInvariant() == "postgresql")
        {
            bool hasHost = connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
                         || connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
                         || connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);
            if (!hasHost)
            {
                return Result.Fail(PersistenceSettingOutcome.Failure.ConnectionStringInvalid);
            }
        }

        return Result.Success();
    }

    private static Result ValidateProvider(PersistenceSetting setting)
    {
        // Validate: only providers in the constraints list are supported.
        if (!PersistenceConstants.Constraints.ValidProviders.Contains(setting.Provider.ToLowerInvariant()))
        {
            return Result.Fail(PersistenceSettingOutcome.Failure.ProviderUnsupported(setting.Provider));
        }

        return Result.Success();
    }

    private static Result ValidateCommandTimeout(PersistenceSetting setting)
    {
        // Validate: timeout must be within the allowed range.
        int min = PersistenceConstants.Constraints.CommandTimeoutSecondsMin;
        int max = PersistenceConstants.Limits.CommandTimeoutSecondsMax;
        if (setting.CommandTimeoutSeconds < min || setting.CommandTimeoutSeconds > max)
        {
            return Result.Fail(PersistenceSettingOutcome.Failure.CommandTimeoutOutOfRange(
                setting.CommandTimeoutSeconds, min, max));
        }

        return Result.Success();
    }

    private static Result ValidateRetryPolicy(PersistenceSetting setting)
    {
        var errors = new List<Error>();

        // Validate: retry attempts must not exceed the limit.
        if (setting.MaxRetryAttempts > PersistenceConstants.Limits.MaxRetryAttemptsLimit)
        {
            errors.Add(PersistenceSettingOutcome.Failure.MaxRetryAttemptsExceeded(
                setting.MaxRetryAttempts, PersistenceConstants.Limits.MaxRetryAttemptsLimit));
        }

        // Validate: base delay must not exceed the limit.
        if (setting.RetryBaseDelayMs > PersistenceConstants.Limits.RetryBaseDelayMsLimit)
        {
            errors.Add(PersistenceSettingOutcome.Failure.RetryBaseDelayExceeded(
                setting.RetryBaseDelayMs, PersistenceConstants.Limits.RetryBaseDelayMsLimit));
        }

        return errors.Count == 0 ? Result.Success() : Result.Fail(errors);
    }

    private static Result ValidatePooling(PersistenceSetting setting)
    {
        var errors = new List<Error>();

        // Validate: max pool size must not exceed the limit.
        if (setting.MaxPoolSize > PersistenceConstants.Limits.MaxPoolSizeLimit)
        {
            errors.Add(PersistenceSettingOutcome.Failure.MaxPoolSizeExceeded(
                setting.MaxPoolSize, PersistenceConstants.Limits.MaxPoolSizeLimit));
        }

        // Validate: min pool size must not exceed max.
        if (setting.MinPoolSize > setting.MaxPoolSize)
        {
            errors.Add(PersistenceSettingOutcome.Failure.PoolSizeInverted);
        }

        return errors.Count == 0 ? Result.Success() : Result.Fail(errors);
    }
}
