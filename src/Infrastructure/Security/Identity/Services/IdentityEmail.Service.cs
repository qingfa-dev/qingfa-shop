using Application.Features.Identity.Auth.Abstractions;

namespace Infrastructure.Security.Identity.Services;

/// <summary>
/// Logging identity email sender. Replace with an SMTP/provider sender in production.
/// </summary>
public sealed class LoggingIdentityEmailSender(
    ILogger<LoggingIdentityEmailSender> logger) : IIdentityEmailSender
{
    /// <inheritdoc />
    public Task<Result> SendConfirmationAsync(
        string email,
        string link,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Identity email confirmation for {Email}: {Link}", email, link);
        return Task.FromResult(Result.Ok());
    }

    /// <inheritdoc />
    public Task<Result> SendPasswordResetAsync(
        string email,
        string link,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Identity password reset for {Email}: {Link}", email, link);
        return Task.FromResult(Result.Ok());
    }
}
