using Microsoft.Extensions.Configuration;

namespace Infrastructure.Caching.Options.Distributed;

/// <summary>Validates distributed cache settings and returns the monad's <see cref="Result{TValue}"/>.</summary>
internal static class DistributedCacheSettingValidation
{
    /// <summary>Validates static rules only (without connection string resolution).</summary>
    public static Result ValidateDistributed(DistributedCacheSetting distributed)
    {
        if (!distributed.Enabled)
        {
            return Result.Success();
        }

        var errors = new List<Error>();

        // Validate: provider is required when distributed cache is enabled
        if (string.IsNullOrWhiteSpace(distributed.Provider))
        {
            errors.Add(DistributedCacheSettingOutcome.Failure.TypeRequired);
        }
        else if (!DistributedCacheSettingConstant.Patterns.ValidTypes.Contains(distributed.Provider!.ToLowerInvariant()))
        {
            errors.Add(DistributedCacheSettingOutcome.Failure.TypeInvalid);
        }

        // Validate: connection string name is required when distributed cache is enabled
        if (string.IsNullOrWhiteSpace(distributed.ConnectionStringName))
        {
            errors.Add(DistributedCacheSettingOutcome.Failure.ConnectionStringNameRequired);
        }

        // Validate: expiration must be within the allowed range
        if (distributed.DefaultExpirationMinutes is < DistributedCacheSettingConstant.Constraints.ExpirationMinutesMin
            or > DistributedCacheSettingConstant.Constraints.ExpirationMinutesMax)
        {
            errors.Add(DistributedCacheSettingOutcome.Failure.ExpirationOutOfRange);
        }

        return errors.Count == 0 ? Result.Success() : Result.Fail(errors);
    }

    /// <summary>Validates including connection string resolution from IConfiguration.</summary>
    public static Result ValidateDistributedWithConnectionString(
        DistributedCacheSetting distributed, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // First validate static rules
        Result staticResult = ValidateDistributed(distributed);
        if (staticResult.IsFailure)
        {
            return staticResult;
        }

        // Resolve and validate the connection string
        if (!distributed.Enabled)
        {
            return Result.Success();
        }

        // Validate: connection string name is required when distributed cache is enabled
        if (string.IsNullOrWhiteSpace(distributed.ConnectionStringName))
        {
            return DistributedCacheSettingOutcome.Failure.ConnectionStringNameRequired;
        }

        string? connectionString = configuration.GetConnectionString(distributed.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Result.Fail(DistributedCacheSettingOutcome.Failure.ConnectionStringNotFound(
                distributed.ConnectionStringName!));
        }

        return Result.Success();
    }
}