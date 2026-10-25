namespace Core.Common.Domain.ValueObjects.Identity;

/// <summary>Validates URLs without modifying them.</summary>
public static class UrlValidator
{
    /// <summary>Returns success when the value is an absolute HTTP or HTTPS URL within the length limit.</summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns>The unchanged URL on success, or its validation errors.</returns>
    public static Result<Url> Validate(Url url)
    {
        return Result<Url>.Success(url)
            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(url.Value),
                error: UrlOutcome.Failure.ValueRequired)

            .Ensure(
                predicate: _ =>
                    url.Value!.Length <= UrlConstant.Constraints.Value.MaxLength,
                error: UrlOutcome.Failure.ValueTooLong)

            .Ensure(
                predicate: _ =>
                    Uri.TryCreate(url.Value, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp ||
                     uri.Scheme == Uri.UriSchemeHttps),
                error: UrlOutcome.Failure.ValueInvalid);
    }
}