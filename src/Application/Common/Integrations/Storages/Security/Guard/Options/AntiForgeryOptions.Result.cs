namespace Application.Common.Integrations.Storages.Security.Guard.Options;

public static class AntiForgeryOptionsResult
{
    public static class Failure
    {
        public static Error MaxConsecutiveFailuresInvalid => Error.Validation(
            code: "Storage.AntiForgery.MaxConsecutiveFailures.Invalid",
            message: $"Storage.AntiForgery.MaxConsecutiveFailures must be between {AntiForgeryOptionsConstant.Constraints.MaxConsecutiveFailuresMin} and {AntiForgeryOptionsConstant.Constraints.MaxConsecutiveFailuresMax}");

        public static Error BlockDurationInvalid => Error.Validation(
            code: "Storage.AntiForgery.BlockDuration.Invalid",
            message: $"Storage.AntiForgery.BlockDuration must be between {AntiForgeryOptionsConstant.Constraints.BlockDurationMin} and {AntiForgeryOptionsConstant.Constraints.BlockDurationMax}");
    }
}
