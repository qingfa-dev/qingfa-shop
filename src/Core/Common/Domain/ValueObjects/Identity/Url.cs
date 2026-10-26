namespace Core.Common.Domain.ValueObjects.Identity;

/// <summary>An absolute HTTP or HTTPS URL retained after trimming whitespace.</summary>
/// <param name="Value">The trimmed URL created by <see cref="Create"/>.</param>
public readonly partial record struct Url(string? Value)
{
    /// <summary>Creates a URL within the configured length limit.</summary>
    /// <param name="value">The candidate absolute HTTP or HTTPS URL; surrounding whitespace is removed.</param>
    /// <returns>The trimmed URL, or a validation error.</returns>
    public static Result<Url> Create(string? value)
    {
        var candidate = new Url(value?.Trim());

        return UrlValidator.Validate(candidate);
    }
}