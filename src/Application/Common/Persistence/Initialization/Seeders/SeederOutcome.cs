namespace Application.Common.Persistence.Initialization.Seeders;

using Core.Common.Monad.Results;

/// <summary>Seeder validation failures.</summary>
public static class SeederOutcome
{
    public static class Failure
    {
        public static Error OrderNegative(string parameterName = "order") => Error.Validation(
            Codes.OrderNegative, $"{parameterName} cannot be negative.");

        public static Error CountNegative(string parameterName = "count") => Error.Validation(
            Codes.CountNegative, $"{parameterName} cannot be negative.");

        public static Error CountExceedsMax(string parameterName = "count") => Error.Validation(
            Codes.CountExceedsMax,
            $"{parameterName} cannot exceed {SeederConstant.Limits.MaxSeeders} seeders.");

        public static Error BatchSizeTooSmall(string parameterName = "batchSize") => Error.Validation(
            Codes.BatchSizeTooSmall,
            $"{parameterName} must be at least {SeederConstant.Limits.MinBatchSize}.");

        public static Error BatchSizeTooLarge(string parameterName = "batchSize") => Error.Validation(
            Codes.BatchSizeTooLarge,
            $"{parameterName} cannot exceed {SeederConstant.Limits.MaxBatchSize}.");
    }

    /// <summary>Stable codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string OrderNegative       = "Seeder.Order.Negative";
        public const string CountNegative       = "Seeder.Count.Negative";
        public const string CountExceedsMax     = "Seeder.Count.ExceedsMax";
        public const string BatchSizeTooSmall   = "Seeder.BatchSize.TooSmall";
        public const string BatchSizeTooLarge   = "Seeder.BatchSize.TooLarge";
    }
}