using Application.Common.Integrations.Notifications.Templates;

namespace Application.Common.Integrations.Notifications.Models;

/// <summary>
/// A fluent builder for constructing <see cref="NotificationMessage"/> and <see cref="NotificationContext"/> instances.
/// </summary>
public static class NotificationMessageBuilder
{
    #region Message Builder Initializers

    /// <summary>
    /// Starts the builder flow by specifying the UseCase first.
    /// </summary>
    /// <param name="useCase">The use case.</param>
    /// <returns>A <see cref="UseCaseBuilder"/> to continue the flow.</returns>
    public static UseCaseBuilder ForUseCase(NotificationUseCase useCase)
    {
        // Create: UseCaseBuilder with the selected use case
        return new UseCaseBuilder(useCase);
    }

    /// <summary>
    /// Creates a base <see cref="NotificationMessage"/> result.
    /// </summary>
    /// <param name="useCase">The use case.</param>
    /// <param name="recipient">The recipient.</param>
    /// <param name="channel">The delivery channel.</param>
    /// <returns>A <see cref="Result{T}"/>.</returns>
    public static Result<NotificationMessage> Create(
        NotificationUseCase useCase,
        NotificationRecipient recipient,
        NotificationChannel channel)
    {
        // Create: NotificationMessage with useCase, recipient, channel, and empty context
        return new NotificationMessage(useCase, recipient, channel, NotificationContext.Empty);
    }

    #endregion

    #region Message Chaining

    /// <summary>Adds delivery metadata to the message result.</summary>
    public static Result<NotificationMessage> WithMetadata(
        this Result<NotificationMessage> result,
           params (string key, object? value)[] metadata)
    {
        // Check: Propagate errors from prior chain step
        if (result.IsFailure) return result.Errors;
        // Assign: Metadata dictionary from the params tuple array
        return result.Value with { Metadata = metadata.ToDictionary() };
    }

    /// <summary>Sets the delivery channel on the message result.</summary>
    public static Result<NotificationMessage> WithChannel(
        this Result<NotificationMessage> result,
        NotificationChannel channel)
    {
        // Check: Propagate errors from prior chain step
        if (result.IsFailure) return result.Errors;
        // Assign: Delivery channel for routing to the correct provider hub
        return result.Value with { Channel = channel };
    }

    /// <summary>Adds a file attachment to the message result.</summary>
    public static Result<NotificationMessage> AddAttachment(
        this Result<NotificationMessage> result,
        NotificationAttachment attachment)
    {
        // Check: Propagate errors from prior chain step
        if (result.IsFailure) return result.Errors;
        // Add: Attachment to the mutable list copy — preserves immutability of original
        List<NotificationAttachment> attachments = result.Value.Attachments != null
            ? [.. result.Value.Attachments]
            : [];

        attachments.Add(attachment);
        return result.Value with { Attachments = attachments };
    }

    /// <summary>Adds a complete context to the message result.</summary>
    public static Result<NotificationMessage> WithContext(
        this Result<NotificationMessage> result,
        NotificationContext context)
    {
        // Check: Propagate errors from prior chain step
        if (result.IsFailure) return result.Errors;
        // Assign: Context object with all template-bound parameters
        return result.Value with { Context = context };
    }

    /// <summary>Adds a single parameter to the message result's context.</summary>
    public static Result<NotificationMessage> AddParam(
        this Result<NotificationMessage> result,
        NotificationParameterType parameter,
        string? value)
    {
        // Check: Propagate errors from prior chain step
        if (result.IsFailure) return result.Errors;
        // Apply: Single parameter onto existing context via merge-or-replace semantics
        NotificationContext newContext = NotificationContext.ApplyParameter(
            result.Value.Context, parameter, value);
        return result.Value with { Context = newContext };
    }

    #endregion

    #region Context Builder

    /// <summary>Creates an empty context result.</summary>
    public static Result<NotificationContext> CreateContext() => NotificationContext.Empty;

    /// <summary>Adds a parameter to a context result.</summary>
    public static Result<NotificationContext> AddParam(
        this Result<NotificationContext> result,
        NotificationParameterType parameter,
        string? value)
    {
        // Check: Propagate errors from prior chain step
        if (result.IsFailure) return result.Errors;
        // Apply: Parameter onto clean or partially built context
        return NotificationContext.ApplyParameter(result.Value, parameter, value);
    }

    /// <summary>Adds multiple parameters to a context result.</summary>
    public static Result<NotificationContext> AddParams(
        this Result<NotificationContext> result,
        IDictionary<NotificationParameterType, string?> parameters)
    {
        // Check: Propagate errors from prior chain step
        if (result.IsFailure) return result.Errors;
        // Apply: All parameters sequentially — last write wins on duplicate keys
        NotificationContext context = result.Value;
        foreach (KeyValuePair<NotificationParameterType, string?> param in parameters)
        {
            context = NotificationContext.ApplyParameter(context, param.Key, param.Value);
        }

        return context;
    }

    #endregion

    /// <summary>
    /// Intermediate builder to support split initialization.
    /// </summary>
    /// <param name="useCase">The notification use case.</param>
    public sealed class UseCaseBuilder(NotificationUseCase useCase)
    {
        /// <summary>Sets the recipient and channel, then returns the message result.</summary>
        /// <param name="recipient">The target recipient.</param>
        /// <param name="channel">The delivery channel.</param>
        /// <returns>A <see cref="Result{T}"/> containing the message.</returns>
        public Result<NotificationMessage> To(
            NotificationRecipient recipient,
            NotificationChannel channel)
        {
            // Create: NotificationMessage with preselected useCase, recipient, and channel
            return new NotificationMessage(useCase, recipient, channel, NotificationContext.Empty);
        }
    }
}
