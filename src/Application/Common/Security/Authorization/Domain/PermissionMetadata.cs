using FluentValidation;
using FluentValidation.Results;

namespace Application.Common.Security.Authorization.Domain;

/// <summary>
/// Describes one canonical permission: <c>domain.category.resource.action</c>.
/// </summary>
/// <remarks>
/// Construction normalizes parts to lowercase and supplies display defaults, but
/// validation remains the responsibility of the factories. Use <see cref="Create"/>
/// or <see cref="TryCreate"/> to obtain a usable permission.
/// </remarks>
public sealed record PermissionMetadata
{
    private static readonly PermissionMetadataValidator s_validator = new();

    /// <summary>Gets the business domain, for example <c>catalog</c>.</summary>
    public string Domain { get; }

    /// <summary>Gets the feature category, for example <c>products</c>.</summary>
    public string Category { get; }

    /// <summary>Gets the protected resource, for example <c>orders</c>.</summary>
    public string Resource { get; }

    /// <summary>Gets the permitted action, for example <c>read</c>.</summary>
    public string Action { get; }

    /// <summary>Gets the canonical dotted identifier.</summary>
    public string Identifier { get; }

    /// <summary>Gets the human-readable display name.</summary>
    public string Name { get; init; }

    /// <summary>Gets the human-readable description.</summary>
    public string Description { get; init; }

    /// <summary>Gets an optional example value describing the permission.</summary>
    public object? Example { get; init; }

    /// <summary>Creates the metadata shape; callers should normally use a validated factory.</summary>
    /// <param name="domain">The business domain.</param>
    /// <param name="category">The feature category.</param>
    /// <param name="resource">The protected resource.</param>
    /// <param name="action">The permitted action.</param>
    /// <param name="name">The optional display name; a default is generated when omitted.</param>
    /// <param name="description">The optional description; a default is generated when omitted.</param>
    /// <param name="example">An optional example value.</param>
    /// <param name="identifier">The optional explicit identifier; a composed identifier is used when omitted.</param>
    public PermissionMetadata(
        string domain,
        string category,
        string resource,
        string action,
        string? name = null,
        string? description = null,
        object? example = null,
        string? identifier = null)
    {
        Domain = PermissionMetadataConstant.NormalizePart(domain);
        Category = PermissionMetadataConstant.NormalizePart(category);
        Resource = PermissionMetadataConstant.NormalizePart(resource);
        Action = PermissionMetadataConstant.NormalizePart(action);

        Name = ResolveText(
            name,
            PermissionMetadataConstant.Compose.DefaultName(Action, Resource));
        Description = ResolveText(
            description,
            PermissionMetadataConstant.Compose.DefaultDescription(Domain, Category, Resource, Action));
        Example = example;
        Identifier = string.IsNullOrWhiteSpace(identifier)
            ? PermissionMetadataConstant.Compose.Identifier(Domain, Category, Resource, Action)
            : PermissionMetadataConstant.NormalizeIdentifier(identifier);
    }

    /// <summary>Creates metadata and throws when a rule is violated.</summary>
    /// <returns>The validated metadata.</returns>
    /// <exception cref="ValidationException">One or more rules failed.</exception>
    public static PermissionMetadata Create(
        string domain,
        string category,
        string resource,
        string action,
        string? name = null,
        string? description = null,
        object? example = null,
        string? identifier = null)
    {
        var metadata = new PermissionMetadata(
            domain,
            category,
            resource,
            action,
            name,
            description,
            example,
            identifier);
        s_validator.ValidateAndThrow(metadata);
        return metadata;
    }

    /// <summary>Parses a canonical identifier and throws when it is invalid.</summary>
    /// <param name="identifier">The dotted identifier to parse.</param>
    /// <returns>The validated metadata.</returns>
    /// <exception cref="FormatException">The identifier is malformed or fails validation.</exception>
    public static PermissionMetadata Parse(string identifier)
    {
        Result<PermissionMetadata> result = TryParse(identifier);

        return result.IsSuccess
            ? result.Value
            : throw new FormatException(result.Errors[0].Message);
    }

    /// <summary>Attempts to parse a canonical identifier without throwing.</summary>
    /// <param name="identifier">The dotted identifier to parse.</param>
    /// <param name="permission">The parsed metadata when successful; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> when parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParse(string identifier, out PermissionMetadata? permission)
    {
        Result<PermissionMetadata> result = TryParse(identifier);
        permission = result.IsSuccess ? result.Value : null;
        return result.IsSuccess;
    }

    /// <summary>Creates metadata and returns validation errors instead of throwing.</summary>
    /// <returns>The validated metadata, or its validation errors.</returns>
    public static Result<PermissionMetadata> TryCreate(
        string domain,
        string category,
        string resource,
        string action,
        string? name = null,
        string? description = null,
        object? example = null,
        string? identifier = null) =>
        Validate(new PermissionMetadata(
            domain,
            category,
            resource,
            action,
            name,
            description,
            example,
            identifier));

    /// <summary>Parses a canonical identifier and returns validation errors instead of throwing.</summary>
    /// <param name="identifier">The dotted identifier to parse.</param>
    /// <returns>The parsed metadata, or its validation errors.</returns>
    public static Result<PermissionMetadata> TryParse(string identifier)
    {
        string normalized = PermissionMetadataConstant.NormalizeIdentifier(identifier);
        if (!PermissionMetadataConstant.IsCanonicalIdentifier(normalized))
            return PermissionMetadataResult.Failure.InvalidIdentifierFormat(identifier);

        string[] parts = normalized.Split('.');
        return TryCreate(parts[0], parts[1], parts[2], parts[3]);
    }

    /// <inheritdoc />
    public override string ToString() => Identifier;

    /// <summary>Converts metadata to its canonical identifier.</summary>
    public static implicit operator string(PermissionMetadata permission) =>
        permission.Identifier;

    private static string ResolveText(string? value, string fallback) =>
        (string.IsNullOrWhiteSpace(value) ? fallback : value).Trim();

    private static Result<PermissionMetadata> Validate(PermissionMetadata metadata)
    {
        ValidationResult validation = s_validator.Validate(metadata);
        if (validation.IsValid)
            return Result<PermissionMetadata>.Ok(metadata);

        List<Error> errors = new(validation.Errors.Count);
        foreach (ValidationFailure failure in validation.Errors)
            errors.Add(Error.Validation(failure.ErrorCode, failure.ErrorMessage));

        return Result<PermissionMetadata>.Fail(errors);
    }
}
