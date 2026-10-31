using FluentEmail.Core;
using FluentEmail.Core.Models;

using Microsoft.Extensions.Options;

using Application.BuildingBlocks.Ports.Notifications.Channels.Emails.Options;
using Application.BuildingBlocks.Ports.Notifications.Models;
using Infrastructure.BuildingBlocks.Adapters.Notifications.Services;
using Infrastructure.BuildingBlocks.Adapters.Notifications.Providers;
using Application.BuildingBlocks.Ports.Notifications.Providers;
using Application.BuildingBlocks.Ports.Notifications.Services;
using Application.BuildingBlocks.Ports.Notifications.Templates;

using Attachment = FluentEmail.Core.Models.Attachment;

namespace Infrastructure.BuildingBlocks.Adapters.Notifications.Channels.Emails.Providers.SendGrid;

/// <summary>Sends email notifications via the SendGrid API.</summary>
public sealed partial class SendGridProvider(
    IOptions<SendGridProviderSetting> providerSettings,
    IOptions<EmailChannelSetting> channelSettings,
    IFluentEmail fluentEmail,
    ILogger<SendGridProvider> logger) : INotificationProvider
{
    private readonly IOptions<SendGridProviderSetting> _providerSettings = providerSettings;
    private readonly IOptions<EmailChannelSetting> _channelSettings = channelSettings;
    private readonly IFluentEmail _fluentEmail = fluentEmail;
    private readonly ILogger<SendGridProvider> _logger = logger;

    public string Name => "SendGrid";

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
                return NotificationProviderOutcome.Failure.RecipientMissing(Name);
            }

            // Guard: Reject missing API key configuration
            if (string.IsNullOrEmpty(_providerSettings.Value.ApiKey))
            {
                return NotificationProviderOutcome.Failure.ConfigurationMissing(Name, nameof(_providerSettings.Value.ApiKey));
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

            // Send: Dispatch email via SendGrid HTTP API
            SendResponse? sendResult = await email.SendAsync(ct);
            if (!sendResult.Successful)
            {
                var errors = string.Join(", ", sendResult.ErrorMessages);
                // Log: Log send failure with error details
                NotificationProvider.Loggers.LogSendFailed(_logger, Name, errors);
                return NotificationProviderOutcome.Failure.SendFailed(Name, errors);
            }

            // Log: Log successful email dispatch
            NotificationProvider.Loggers.LogSendSuccess(_logger, Name, message.Recipient.Identifier);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            // Catch: Handle and log SendGrid API failure
            NotificationProvider.Loggers.LogSendException(_logger, Name, ex);
            return Result.Fail(NotificationProviderOutcome.Failure.SendFailed(Name, ex.Message));
        }
    }
}
