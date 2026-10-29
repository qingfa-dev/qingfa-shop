namespace Application.Common.Integrations.Storages.Security.Guard.Options;

public sealed class AntiForgeryOptions
{
    public const string SectionName = "Storage:AntiForgery";

    public int MaxConsecutiveFailures { get; set; } = AntiForgeryOptionsConstant.Defaults.MaxConsecutiveFailures;

    public TimeSpan BlockDuration { get; set; } = AntiForgeryOptionsConstant.Defaults.BlockDuration;
}
