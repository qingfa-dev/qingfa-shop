using System.Text.RegularExpressions;

namespace Core.Common.Concerns.Content.MediaAttachable;

/// <summary>Validator for the <see cref="IMediaAttachable{TMedia}"/> concern.</summary>
public static class MediaAttachableValidator
{
    #region Public Methods

    /// <summary>
    /// Validates the media that <see cref="MediaAttachableExtensions.AddMedia{TValue, TMedia}"/>
    /// is about to attach, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the media-attachable entity.</typeparam>
    /// <typeparam name="TMedia">The type of the media.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="media">The media to attach.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateAddMedia<TValue, TMedia>(
        TValue auditable,
        TMedia media)
        where TValue : IMediaAttachable<TMedia>
    {
        // Guard: Entity must not be null.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                MediaAttachableOutcome.Failure.EntityRequired);
        }

        // Guard: Media must not be null.
        if (media is null)
        {
            return Result<TValue>.Fail(
                MediaAttachableOutcome.Failure.MediaRequired);
        }

        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: entity => !entity.Media.Contains(media),
                error: MediaAttachableOutcome.Failure.MediaDuplicate)

            .Ensure(
                predicate: entity =>
                    entity.Media.Count <
                    MediaAttachableConstant.Constraints.Collection.MaxAttachments,
                error: MediaAttachableOutcome.Failure.MediaTooMany);
    }

    /// <summary>
    /// Validates that the media that
    /// <see cref="MediaAttachableExtensions.RemoveMedia{TValue, TMedia}"/>
    /// targets is attached, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the media-attachable entity.</typeparam>
    /// <typeparam name="TMedia">The type of the media.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="media">The media to detach.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateRemoveMedia<TValue, TMedia>(
        TValue auditable,
        TMedia media)
        where TValue : IMediaAttachable<TMedia>
    {
        // Guard: Entity must not be null.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                MediaAttachableOutcome.Failure.EntityRequired);
        }

        // Guard: Media must not be null.
        if (media is null)
        {
            return Result<TValue>.Fail(
                MediaAttachableOutcome.Failure.MediaRequired);
        }

        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: entity => entity.Media.Contains(media),
                error: MediaAttachableOutcome.Failure.MediaNotFound);
    }

    /// <summary>
    /// Validates that the entity that
    /// <see cref="MediaAttachableExtensions.ClearMedia{TValue, TMedia}"/>
    /// is about to clear exists, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the media-attachable entity.</typeparam>
    /// <typeparam name="TMedia">The type of the media.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateClearMedia<TValue, TMedia>(
        TValue auditable)
        where TValue : IMediaAttachable<TMedia>
    {
        // Guard: Entity must not be null.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                MediaAttachableOutcome.Failure.EntityRequired);
        }

        return Result<TValue>.Success(auditable);
    }

    /// <summary>Validates a media URL without mutating any entity.</summary>
    /// <param name="url">The media URL to validate.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result ValidateMediaUrl(string? url)
    {
        // Guard: URL must be specified.
        if (string.IsNullOrWhiteSpace(url))
        {
            return Result.Fail(MediaAttachableOutcome.Failure.UrlRequired);
        }

        // Guard: URL must not exceed the maximum length.
        if (url.Length > MediaAttachableConstant.Constraints.Url.MaxLength)
        {
            return Result.Fail(MediaAttachableOutcome.Failure.UrlTooLong);
        }

        // Guard: URL must be a valid http(s) URL.
        if (!Regex.IsMatch(url, MediaAttachableConstant.Patterns.Url))
        {
            return Result.Fail(MediaAttachableOutcome.Failure.UrlInvalid);
        }

        return Result.Success();
    }

    /// <summary>Validates media alt text without mutating any entity.</summary>
    /// <param name="altText">The alt text to validate (null or empty is allowed).</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result ValidateMediaAltText(string? altText)
    {
        // Guard: Alt text must not exceed the maximum length.
        if (altText is not null &&
            altText.Length > MediaAttachableConstant.Constraints.AltText.MaxLength)
        {
            return Result.Fail(MediaAttachableOutcome.Failure.AltTextTooLong);
        }

        return Result.Success();
    }

    /// <summary>Validates a media type discriminator without mutating any entity.</summary>
    /// <param name="mediaType">The media type to validate.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result ValidateMediaType(string? mediaType)
    {
        // Guard: Media type must be specified.
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            return Result.Fail(MediaAttachableOutcome.Failure.MediaTypeRequired);
        }

        // Guard: Media type must be in the allowed list.
        if (!MediaAttachableConstant.Defaults.MediaType.Allowed.Contains(
                mediaType.Trim().ToLowerInvariant()))
        {
            return Result.Fail(MediaAttachableOutcome.Failure.MediaTypeInvalid);
        }

        return Result.Success();
    }

    #endregion
}