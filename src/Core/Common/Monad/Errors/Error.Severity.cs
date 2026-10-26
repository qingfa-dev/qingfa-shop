namespace Core.Common.Monad.Errors;

/// <summary>
/// Represents the operational severity level of an error.
/// </summary>
/// <remarks>
/// Severity drives observability routing (logging, alerting, escalation) and
/// influences the default HTTP status code when an error is converted to a response.
/// </remarks>
public enum ErrorSeverity
{
    /// <summary>
    /// Informational; does not indicate a failure condition.
    /// </summary>
    Info = 0,

    /// <summary>
    /// A potential issue that did not prevent the operation from completing.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// A failure that prevented the operation from completing successfully.
    /// </summary>
    Error = 2,

    /// <summary>
    /// A severe failure requiring immediate attention (e.g., data corruption, security breach).
    /// </summary>
    Critical = 3,
}