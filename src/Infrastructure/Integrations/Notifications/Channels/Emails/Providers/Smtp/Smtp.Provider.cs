using FluentEmail.Core;
using FluentEmail.Core.Models;

using Microsoft.Extensions.Options;

using Application.Common.Integrations.Notifications.Channels.Emails.Options;
using Application.Common.Integrations.Notifications.Models;
using Infrastructure.Integrations.Notifications.Services;
using Infrastructure.Integrations.Notifications.Providers;
using Application.Common.Integrations.Notifications.Providers;
using Application.Common.Integrations.Notifications.Services;
using Application.Common.Integrations.Notifications.Templates;

using Attachment = FluentEmail.Core.Models.Attachment;

namespace Infrastructure.Integrations.Notifications.Channels.Emails.Providers.Smtp;

/// <summary>Sends email notifications via the SMTP protocol.</summary>
public sealed partial class SmtpProvider(
    IOptions<SmtpProviderSetting> providerSettings,
    IOptions<EmailChannelSetting> channelSettings,
    IFluentEmail fluentEmail,
    ILogger<SmtpProvider> logger) : INotificationProvider
{
    private readonly IOptions<SmtpProviderSetting> _providerSettings = providerSettings;
    private readonly IOptions<EmailChannelSetting> _channelSettings = channelSettings;
    private readonly IFluentEmail _fluentEmail = fluentEmail;
    private readonly ILogger<SmtpProvider> _logger = logger;

    public string Name => "Smtp";

    public int Priority => _providerSettings.Value.Priority;

    public bool IsEnabled => _providerSettings.Value.Enabled;

    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<Result> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        try
        {
            // Guard: Reject null or empty recipient identifier
            if (string.IsNullOrEmpty(message.Recipient.Identifier))
            {
                return NotificationProviderResult.Failure.RecipientMissing(Name);
            }

            // Guard: Reject missing SMTP host configuration
            if (string.IsNullOrEmpty(_providerSettings.Value.Host))
            {
                return NotificationProviderResult.Failure.ConfigurationMissing(Name, nameof(_providerSettings.Value.Host));
            }

            // Guard: Reject invalid or zero port configuration
            if (_providerSettings.Value.Port <= 0)
            {
                return NotificationProviderResult.Failure.ConfigurationMissing(Name, nameof(_providerSettings.Value.Port));
            }

            // Guard: Reject missing username when not using default credentials
            if (!_providerSettings.Value.UseDefaultCredentials && string.IsNullOrEmpty(_providerSettings.Value.Username))
            {
                return NotificationProviderResult.Failure.ConfigurationMissing(Name, nameof(_providerSettings.Value.Username));
            }

            // Log: Trace outbound email attempt with provider metadata
            NotificationProvider.Loggers.LogSending(
                _logger,
                Name,
                Channel,
                NotificationPriorityLevel.Normal,
                "en",
                message.Recipient.Identifier);

            // Map: Render template placeholders into final content
            Result<NotificationContent> contentResult = message.MapContent();
            if (contentResult.IsFailure)
            {
                // Log: Log content mapping failure
                NotificationProvider.Loggers.LogSendFailed(_logger, Name, contentResult.Errors[0].Message);
                return contentResult.Errors;
            }

            IFluentEmail email = _fluentEmail
                .SetFrom(_channelSettings.Value.FromEmail, _channelSettings.Value.FromName)
                .To(message.Recipient.Identifier, message.Recipient.Name)
                .Subject(contentResult.Value.Subject)
                .PlaintextAlternativeBody(contentResult.Value.Body)
                .Body(contentResult.Value.HtmlBody, true);

            if (message.Attachments is { Count: > 0 })
            {
                foreach (NotificationAttachment attachment in message.Attachments)
                {
                    email.Attach(new Attachment
                    {
                        Filename = attachment.FileName,
                        Data = new MemoryStream(attachment.Data),
                        ContentType = attachment.ContentType
                    });
                }
            }

            // Send: Dispatch email via SMTP protocol
            SendResponse? sendResult = await email.SendAsync(ct);
            if (!sendResult.Successful)
            {
                var errors = string.Join(", ", sendResult.ErrorMessages);
                // Log: Log send failure with error details
                NotificationProvider.Loggers.LogSendFailed(_logger, Name, errors);
                return NotificationProviderResult.Failure.SendFailed(Name, errors);
            }

            // Log: Log successful email dispatch
            NotificationProvider.Loggers.LogSendSuccess(_logger, Name, message.Recipient.Identifier);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            // Catch: Handle and log SMTP delivery failure
            NotificationProvider.Loggers.LogSendException(_logger, Name, ex);
            return Result.Fail(NotificationProviderResult.Failure.SendFailed(Name, ex.Message));
        }
    }
}
