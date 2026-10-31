using System.Text.RegularExpressions;

using Application.BuildingBlocks.Ports.Notifications.Models;
using Application.BuildingBlocks.Ports.Notifications.Options;
using Application.BuildingBlocks.Ports.Notifications.Store;
using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Infrastructure.BuildingBlocks.Adapters.Notifications.Services;

/// <summary>
/// Provides extension methods for mapping <see cref="NotificationMessage"/> instances to final <see cref="NotificationContent"/>.
/// </summary>
public static class NotificationMapper
{
    private static readonly Regex PlaceholderRegex = new(@"\{(\w+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Maps a <see cref="NotificationMessage"/> to <see cref="NotificationContent"/> by filling template placeholders with context values.
    /// </summary>
    /// <param name="message">The notification request.</param>
    /// <returns>A <see cref="Result{T}"/> containing the rendered content or failures.</returns>
    public static Result<NotificationContent> MapContent(this NotificationMessage message)
    {
        if (!NotificationStore.Templates.TryGetValue(message.UseCase, out NotificationTemplate? template))
        {
            return NotificationOutcome.Failure.TemplateNotFound(message.UseCase.ToString());
        }

        var subject = FillTemplate(template.Name, message.Context);
        var body = FillTemplate(template.TemplateContent ?? string.Empty, message.Context);
        var htmlBody = FillTemplate(template.HtmlTemplateContent ?? string.Empty, message.Context);

        return NotificationContent.Create(subject, body, string.IsNullOrWhiteSpace(htmlBody) ? null : htmlBody);
    }

    /// <summary>
    /// Fills missing system parameters in the <see cref="NotificationMessage"/> context with default values from <see cref="NotificationSetting"/>.
    /// </summary>
    /// <param name="message">The notification message.</param>
    /// <param name="defaults">The default options.</param>
    /// <returns>A new message with updated context if changes were made; otherwise, the original message.</returns>
    public static NotificationMessage ApplyDefaults(this NotificationMessage message, NotificationSetting defaults)
    {
        NotificationContext context = message.Context;
        var hasChanges = false;

        foreach (NotificationParameterType paramType in GetAllSystemParameterTypes())
        {
            var currentValue = context.GetValue(paramType);
            if (string.IsNullOrEmpty(currentValue))
            {
                var defaultValue = paramType switch
                {
                    NotificationParameterType.ApplicationName => defaults.ApplicationName,
                    NotificationParameterType.SupportEmail => defaults.SupportEmail,
                    NotificationParameterType.SupportPhone => defaults.SupportPhone,
                    NotificationParameterType.ApplicationUrl => defaults.ApplicationUrl,
                    NotificationParameterType.UnsubscribeUrl => defaults.UnsubscribeUrl,
                    _ => null
                };

                if (!string.IsNullOrEmpty(defaultValue))
                {
                    context = NotificationContext.ApplyParameter(context, paramType, defaultValue);
                    hasChanges = true;
                }
            }
        }

        return hasChanges ? message with { Context = context } : message;
    }

    /// <summary>
    /// Maps a <see cref="NotificationPriorityLevel"/> to its corresponding background job queue name.
    /// </summary>
    /// <param name="priority">The priority level.</param>
    /// <returns>The queue name.</returns>
    public static string ToQueueName(this NotificationPriorityLevel priority) => priority switch
    {
        NotificationPriorityLevel.Critical => NotificationSettingConstant.BackgroundJobs.Queues.Critical,
        NotificationPriorityLevel.High => NotificationSettingConstant.BackgroundJobs.Queues.High,
        NotificationPriorityLevel.Normal => NotificationSettingConstant.BackgroundJobs.Queues.Default,
        NotificationPriorityLevel.Low => NotificationSettingConstant.BackgroundJobs.Queues.Low,
        _ => NotificationSettingConstant.BackgroundJobs.Queues.Default
    };

    private static string FillTemplate(string template, NotificationContext context)
    {
        if (string.IsNullOrWhiteSpace(template)) return template;

        return PlaceholderRegex.Replace(template, match =>
        {
            var keyName = match.Groups[1].Value;
            if (Enum.TryParse(keyName, out NotificationParameterType param))
            {
                return context.GetValue(param) ?? match.Value;
            }
            return match.Value;
        });
    }

    private static IEnumerable<NotificationParameterType> GetAllSystemParameterTypes()
    {
        yield return NotificationParameterType.ApplicationName;
        yield return NotificationParameterType.ApplicationUrl;
        yield return NotificationParameterType.SupportEmail;
        yield return NotificationParameterType.SupportPhone;
        yield return NotificationParameterType.UnsubscribeUrl;
    }
}
