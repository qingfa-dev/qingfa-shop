namespace Application.BuildingBlocks.Ports.Storage.Security.Guard.Options;

public static class AntiForgeryOptionsOutcome
{
    public static class Failure
    {
        public static Error MaxConsecutiveFailuresInvalid => Error.Validation(
            code: Codes.MaxConsecutiveFailuresInvalid,
            message: $"Storage.AntiForgery.MaxConsecutiveFailures must be between {AntiForgeryOptionsConstant.Constraints.MaxConsecutiveFailuresMin} and {AntiForgeryOptionsConstant.Constraints.MaxConsecutiveFailuresMax}");

        public static Error BlockDurationInvalid => Error.Validation(
            code: Codes.BlockDurationInvalid,
            message: $"Storage.AntiForgery.BlockDuration must be between {AntiForgeryOptionsConstant.Constraints.BlockDurationMin} and {AntiForgeryOptionsConstant.Constraints.BlockDurationMax}");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string MaxConsecutiveFailuresInvalid = "Storage.AntiForgery.MaxConsecutiveFailures.Invalid";
        public const string BlockDurationInvalid = "Storage.AntiForgery.BlockDuration.Invalid";
    }
}
