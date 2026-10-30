using System.Collections.Concurrent;

using Application.Features.Identity.Auth.Abstractions;

using Core.Common.Monad.Results;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Testing.Fixtures;

namespace Api.Integration.Tests;

/// <summary>Boots the real identity services and records outbound identity email.</summary>
/// <remarks>
/// Only the email transport is replaced. Everything else under test is the production
/// ASP.NET Core Identity pipeline, so login, token rotation, and password reset run
/// against the migrated PostgreSQL database.
/// </remarks>
public sealed class IdentityStackFactory : ShopApiFactory
{
    /// <summary>The recording transport installed into the host.</summary>
    public RecordingIdentityEmailSender Email { get; } = new();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IIdentityEmailSender>();
            services.AddSingleton<IIdentityEmailSender>(Email);
        });
    }
}

/// <summary>Captures confirmation and reset links so tests can complete the flows.</summary>
public sealed class RecordingIdentityEmailSender : IIdentityEmailSender
{
    private readonly ConcurrentQueue<RecordedIdentityEmail> _messages = new();

    /// <summary>The messages recorded so far, oldest first.</summary>
    public IReadOnlyList<RecordedIdentityEmail> Messages => [.. _messages];

    /// <summary>Returns the latest message for an address and kind, or <c>null</c>.</summary>
    /// <param name="email">The recipient address.</param>
    /// <param name="kind">The message kind.</param>
    /// <returns>The matching recorded message.</returns>
    public RecordedIdentityEmail? Latest(string email, RecordedIdentityEmailKind kind) =>
        _messages.LastOrDefault(message =>
            string.Equals(message.Email, email, StringComparison.OrdinalIgnoreCase) &&
            message.Kind == kind);

    /// <summary>Removes every recorded message.</summary>
    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }

    /// <inheritdoc />
    public Task<Result> SendConfirmationAsync(
        string email,
        string link,
        CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(new RecordedIdentityEmail(
            email,
            link,
            RecordedIdentityEmailKind.Confirmation));

        return Task.FromResult(Result.Ok());
    }

    /// <inheritdoc />
    public Task<Result> SendPasswordResetAsync(
        string email,
        string link,
        CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(new RecordedIdentityEmail(
            email,
            link,
            RecordedIdentityEmailKind.PasswordReset));

        return Task.FromResult(Result.Ok());
    }
}

/// <summary>One identity email captured by <see cref="RecordingIdentityEmailSender"/>.</summary>
/// <param name="Email">The recipient address.</param>
/// <param name="Link">The confirmation or reset link.</param>
/// <param name="Kind">The message kind.</param>
public sealed record RecordedIdentityEmail(
    string Email,
    string Link,
    RecordedIdentityEmailKind Kind);

/// <summary>The identity email flows the recorder distinguishes.</summary>
public enum RecordedIdentityEmailKind
{
    /// <summary>An email confirmation link.</summary>
    Confirmation,

    /// <summary>A password reset link.</summary>
    PasswordReset,
}
