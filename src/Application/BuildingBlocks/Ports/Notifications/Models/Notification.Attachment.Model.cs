namespace Application.BuildingBlocks.Ports.Notifications.Models;

/// <summary>
/// Represents a file attachment associated with a notification (typically for Email).
/// </summary>
public sealed record NotificationAttachment
{
    /// <summary>Gets the name of the file including its extension.</summary>
    public string FileName { get; init; }

    /// <summary>Gets the raw binary data of the file.</summary>
    public byte[] Data { get; init; }

    /// <summary>Gets the MIME content type (e.g., "application/pdf").</summary>
    public string ContentType { get; init; }

    private NotificationAttachment(string fileName, byte[] data, string contentType)
    {
        FileName = fileName;
        Data = data;
        ContentType = contentType;
    }

    /// <summary>
    /// Creates a new instance of <see cref="NotificationAttachment"/>.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="data">The file data.</param>
    /// <param name="contentType">The MIME type.</param>
    /// <returns>A new <see cref="NotificationAttachment"/>.</returns>
    public static NotificationAttachment Create(
        string fileName,
        byte[] data,
        string contentType)
        => new(fileName, data, contentType);
}
