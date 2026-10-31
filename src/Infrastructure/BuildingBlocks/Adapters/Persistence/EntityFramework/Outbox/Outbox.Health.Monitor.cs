using Application.BuildingBlocks.Messaging.Outbox;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

/// <summary>Reports outbox lag as a readiness signal.</summary>
public sealed class OutboxHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxSetting _setting;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the health check.</summary>
    public OutboxHealthCheck(
        IServiceScopeFactory scopeFactory,
        OutboxSetting setting,
        TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _setting = setting ?? throw new ArgumentNullException(nameof(setting));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_setting.Enabled)
            return HealthCheckResult.Healthy("Outbox is disabled.");

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();

            Result<int> count = await store.CountPendingAsync(cancellationToken).ConfigureAwait(false);
            if (count.IsFailure)
                return HealthCheckResult.Degraded(count.Errors[0].Message);

            if (count.Value == 0)
                return HealthCheckResult.Healthy("Outbox is empty.");

            Result<DateTimeOffset?> oldest = await store
                .OldestPendingOnUtcAsync(cancellationToken)
                .ConfigureAwait(false);

            if (oldest.IsFailure || oldest.Value is null)
                return HealthCheckResult.Degraded("Outbox pending age is unavailable.");

            TimeSpan age = _timeProvider.GetUtcNow() - oldest.Value.Value;
            return age > TimeSpan.FromSeconds(_setting.MaxAgeSeconds)
                ? HealthCheckResult.Degraded(
                    $"{count.Value} pending outbox message(s); oldest is {age.TotalSeconds:F0}s old.")
                : HealthCheckResult.Healthy(
                    $"{count.Value} pending outbox message(s); oldest is {age.TotalSeconds:F0}s old.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Degraded($"Outbox health check failed: {exception.Message}");
        }
    }
}
