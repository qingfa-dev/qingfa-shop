namespace Core.BuildingBlocks.Modeling.Concerns.Content.MediaAttachable;

/// <summary>Extension methods for the <see cref="IMediaAttachable{TMedia}"/> concern.</summary>
public static class MediaAttachableExtensions
{
    #region Public Methods

    /// <summary>
    /// Attaches media to the entity.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The type of the media-attachable entity.</typeparam>
    /// <typeparam name="TMedia">The type of the media.</typeparam>
    /// <param name="result">The result containing the media-attachable entity.</param>
    /// <param name="media">The media to attach.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> AddMedia<TValue, TMedia>(
        this Result<TValue> result,
        TMedia media)
        where TValue : IMediaAttachable<TMedia>
    {
        return result
            .Bind(entity =>
                MediaAttachableValidator.ValidateAddMedia(entity, media))
            .Tap(entity =>
            {
                entity.Media.Add(media);
            });
    }

    /// <summary>
    /// Detaches media from the entity.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The type of the media-attachable entity.</typeparam>
    /// <typeparam name="TMedia">The type of the media.</typeparam>
    /// <param name="result">The result containing the media-attachable entity.</param>
    /// <param name="media">The media to detach.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> RemoveMedia<TValue, TMedia>(
        this Result<TValue> result,
        TMedia media)
        where TValue : IMediaAttachable<TMedia>
    {
        return result
            .Bind(entity =>
                MediaAttachableValidator.ValidateRemoveMedia(entity, media))
            .Tap(entity =>
            {
                entity.Media.Remove(media);
            });
    }

    /// <summary>
    /// Detaches all media from the entity.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The type of the media-attachable entity.</typeparam>
    /// <typeparam name="TMedia">The type of the media.</typeparam>
    /// <param name="result">The result containing the media-attachable entity.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> ClearMedia<TValue, TMedia>(
        this Result<TValue> result)
        where TValue : IMediaAttachable<TMedia>
    {
        return result
            .Bind(entity =>
                MediaAttachableValidator.ValidateClearMedia<TValue, TMedia>(entity))
            .Tap(entity =>
            {
                entity.Media.Clear();
            });
    }

    /// <summary>Normalizes a media URL by trimming surrounding whitespace.</summary>
    /// <param name="url">The media URL to normalize.</param>
    /// <returns>The normalized URL, or null when the input is null.</returns>
    public static string? NormalizeUrl(string? url)
    {
        return url?.Trim();
    }

    /// <summary>Normalizes media alt text by trimming surrounding whitespace.</summary>
    /// <param name="altText">The alt text to normalize.</param>
    /// <returns>The normalized alt text, or null when the input is null.</returns>
    public static string? NormalizeAltText(string? altText)
    {
        return altText?.Trim();
    }

    /// <summary>Normalizes a media type by trimming whitespace and lowercasing.</summary>
    /// <param name="mediaType">The media type to normalize.</param>
    /// <returns>The normalized media type, or null when the input is null.</returns>
    public static string? NormalizeMediaType(string? mediaType)
    {
        return mediaType?.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes media metadata (URL, alt text, media type) in one step.
    /// </summary>
    /// <param name="url">The media URL to normalize.</param>
    /// <param name="altText">The alt text to normalize.</param>
    /// <param name="mediaType">The media type to normalize.</param>
    /// <returns>The normalized (URL, alt text, media type) tuple.</returns>
    public static (string? Url, string? AltText, string? MediaType) NormalizeMedia(
        string? url,
        string? altText,
        string? mediaType)
    {
        return (
            NormalizeUrl(url),
            NormalizeAltText(altText),
            NormalizeMediaType(mediaType));
    }

    #endregion
}