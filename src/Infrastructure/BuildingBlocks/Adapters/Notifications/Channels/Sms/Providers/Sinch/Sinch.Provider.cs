using Microsoft.Extensions.Options;

using Application.BuildingBlocks.Ports.Notifications.Models;
using Infrastructure.BuildingBlocks.Adapters.Notifications.Services;
using Infrastructure.BuildingBlocks.Adapters.Notifications.Providers;
using Application.BuildingBlocks.Ports.Notifications.Providers;
using Application.BuildingBlocks.Ports.Notifications.Services;
using Application.BuildingBlocks.Ports.Notifications.Templates;

using Sinch;
using Sinch.SMS.Batches.Send;

namespace Infrastructure.BuildingBlocks.Adapters.Notifications.Channels.Sms.Providers.Sinch;

/// <summary>Sends SMS notifications via the Sinch API.</summary>
public sealed partial class SinchProvider(
    IOptions<SinchProviderSetting> settings,
    ISinchClient sinchClient,
    ILogger<SinchProvider> logger) : INotificationProvider
{
    private readonly IOptions<SinchProviderSetting> _settings = settings;
    private readonly ISinchClient _sinchClient = sinchClient;
    private readonly ILogger<SinchProvider> _logger = logger;

    public string Name => "Sinch";

    public int Priority => _settings.Value.Priority;

    public bool IsEnabled => _settings.Value.Enabled;

    public NotificationChannel Channel => NotificationChannel.SMS;

    public async Task<Result> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        try
        {
            // Guard: Reject null or empty recipient identifier
            if (string.IsNullOrEmpty(message.Recipient.Identifier))
            {
                return NotificationProviderOutcome.Failure.RecipientMissing(Name);
            }

            // Guard: Reject missing sender phone number configuration
            if (string.IsNullOrEmpty(_settings.Value.SenderPhoneNumber))
            {
                return NotificationProviderOutcome.Failure.ConfigurationMissing(Name, nameof(_settings.Value.SenderPhoneNumber));
            }

            // Log: Trace outbound SMS attempt with provider metadata
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
                // Log:
                NotificationProvider.Loggers.LogSendFailed(_logger, Name, contentResult.Errors[0].Message);
                return contentResult.Errors;
            }

            var batchRequest = new SendTextBatchRequest
            {
                From = _settings.Value.SenderPhoneNumber,
                To = [message.Recipient.Identifier],
                Body = contentResult.Value.Body
            };

            // Send: Dispatch SMS via Sinch REST API
            await _sinchClient.Sms.Batches.Send(batchRequest, ct);

            // Log:
            NotificationProvider.Loggers.LogSendSuccess(_logger, Name, message.Recipient.Identifier);

            return Result.Ok();
        }
        // Catch: Handle and log Sinch API failure
        catch (Exception ex)
        {
            NotificationProvider.Loggers.LogSendException(_logger, Name, ex);
            return Result.Fail(NotificationProviderOutcome.Failure.SendFailed(Name, ex.Message));
        }
    }
}
