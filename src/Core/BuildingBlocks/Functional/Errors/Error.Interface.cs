namespace Core.BuildingBlocks.Functional.Errors;

/// <summary>
/// Defines the contract for a domain or infrastructure error.
/// </summary>
/// <remarks>
/// Implementations must provide a machine-readable code, a human-readable message,
/// and optional Problem Details fields. The <see cref="Severity"/> property enables
/// observability routing and escalation logic.
/// </remarks>
public interface IError : IHasMetadata<DefaultMetadata>
{
    /// <summary>
    /// Gets the machine-readable error code.
    /// </summary>
    /// <value>
    /// A stable identifier (e.g., "order.not_found") used for programmatic handling.
    /// </value>
    string Code { get; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    /// <value>
    /// A description suitable for display to end users or in logs.
    /// </value>
    string Message { get; }

    /// <summary>
    /// Gets the optional error type category (e.g., a URI identifying the error class).
    /// </summary>
    string? Type { get; }

    /// <summary>
    /// Gets the optional unique instance identifier for this error occurrence.
    /// </summary>
    string? Instance { get; }

    /// <summary>
    /// Gets the optional HTTP status code associated with the error.
    /// </summary>
    int? Status { get; }

    /// <summary>
    /// Gets the severity level of the error.
    /// </summary>
    ErrorSeverity Severity { get; }
}