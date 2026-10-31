namespace Application.BuildingBlocks.Ports.Storage.Security.Guard.Options;

public static class AntiForgeryOptionsConstant
{
    public static class Defaults
    {
        public const int MaxConsecutiveFailures = 5;

        public static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(15);
    }

    public static class Constraints
    {
        public const int MaxConsecutiveFailuresMin = 1;

        public const int MaxConsecutiveFailuresMax = 100;

        public static readonly TimeSpan BlockDurationMin = TimeSpan.FromSeconds(1);

        public static readonly TimeSpan BlockDurationMax = TimeSpan.FromDays(1);
    }
}
