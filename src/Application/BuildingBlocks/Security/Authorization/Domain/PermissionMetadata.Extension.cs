namespace Application.BuildingBlocks.Security.Authorization.Domain;

/// <summary>
/// Immutable customization helpers for <see cref="PermissionMetadata"/>.
/// </summary>
public static class PermissionMetadataExtension
{
    /// <summary>Returns a copy with a validated display name.</summary>
    /// <param name="metadata">The source metadata.</param>
    /// <param name="name">The replacement display name.</param>
    /// <returns>The updated metadata.</returns>
    /// <exception cref="ArgumentException">The name is blank or too long.</exception>
    public static PermissionMetadata WithName(
        this PermissionMetadata metadata,
        string name) =>
        Unwrap(metadata.TryWithName(name), nameof(name));

    /// <summary>Returns a copy with a validated description.</summary>
    /// <param name="metadata">The source metadata.</param>
    /// <param name="description">The replacement description.</param>
    /// <returns>The updated metadata.</returns>
    /// <exception cref="ArgumentException">The description is blank or too long.</exception>
    public static PermissionMetadata WithDescription(
        this PermissionMetadata metadata,
        string description) =>
        Unwrap(metadata.TryWithDescription(description), nameof(description));

    /// <summary>Returns a copy with the supplied example value.</summary>
    /// <param name="metadata">The source metadata.</param>
    /// <param name="example">The replacement example; <c>null</c> clears it.</param>
    /// <returns>The updated metadata.</returns>
    public static PermissionMetadata WithExample(
        this PermissionMetadata metadata,
        object? example)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return metadata with { Example = example };
    }

    /// <summary>Attempts to replace the display name without throwing.</summary>
    /// <param name="metadata">The source metadata.</param>
    /// <param name="name">The replacement display name.</param>
    /// <returns>The updated metadata, or its validation errors.</returns>
    public static Result<PermissionMetadata> TryWithName(
        this PermissionMetadata metadata,
        string name)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (string.IsNullOrWhiteSpace(name))
            return PermissionMetadataOutcome.Failure.PartRequired(nameof(PermissionMetadata.Name));

        if (name.Length > PermissionMetadataConstant.Constraints.MaxNameLength)
            return PermissionMetadataOutcome.Failure.NameTooLong(name.Length);

        return Result<PermissionMetadata>.Ok(metadata with { Name = name.Trim() });
    }

    /// <summary>Attempts to replace the description without throwing.</summary>
    /// <param name="metadata">The source metadata.</param>
    /// <param name="description">The replacement description.</param>
    /// <returns>The updated metadata, or its validation errors.</returns>
    public static Result<PermissionMetadata> TryWithDescription(
        this PermissionMetadata metadata,
        string description)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (string.IsNullOrWhiteSpace(description))
            return PermissionMetadataOutcome.Failure.PartRequired(nameof(PermissionMetadata.Description));

        if (description.Length > PermissionMetadataConstant.Constraints.MaxDescriptionLength)
            return PermissionMetadataOutcome.Failure.DescriptionTooLong(description.Length);

        return Result<PermissionMetadata>.Ok(metadata with { Description = description.Trim() });
    }

    private static PermissionMetadata Unwrap(
        Result<PermissionMetadata> result,
        string parameterName) =>
        // Boundary: the With* methods are the throwing convenience API; use TryWith* in flow.
        result.IsSuccess
            ? result.Value
            : throw new ArgumentException(result.Errors[0].Message, parameterName);
}
