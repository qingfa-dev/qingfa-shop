namespace Core.Common.Monad.Results;

/// <summary>
/// Predefined error representations used by <see cref="Result"/> validation.
/// </summary>
/// <remarks>
/// Centralizes error codes and messages for result validation failures
/// to ensure consistent error handling across the codebase.
/// </remarks>
public static class ResultOutcome
{
    #region Failure

    public static class Failure
    {
        #region Errors

        /// <summary>
        /// Error list validation failures.
        /// </summary>
        public static class Errors
        {
            /// <summary>
            /// Failure result requires at least one error.
            /// </summary>
            public static Error Empty =>
                Error.InternalServerError(
                    "result.errors.empty",
                    "At least one error is required.");

            /// <summary>
            /// Result exceeds maximum error count.
            /// </summary>
            /// <param name="maxCount">The maximum allowed error count.</param>
            public static Error ExceedsMaxCount(int maxCount) =>
                Error.InternalServerError(
                    "result.errors.exceeds_max_count",
                    $"A result cannot contain more than {maxCount} errors.");

            /// <summary>
            /// All errors in a result must have the same HTTP status code.
            /// </summary>
            public static Error MixedStatusCodes =>
                Error.InternalServerError(
                    "result.errors.mixed_status_codes",
                    "All errors in a result must have the same HTTP status code.");
        }

        #endregion

        #region Metadata

        /// <summary>
        /// Metadata validation failures.
        /// </summary>
        public static class Metadata
        {
            /// <summary>
            /// Metadata keys cannot be null, empty, or whitespace.
            /// </summary>
            public static Error EmptyKey =>
                Error.InternalServerError(
                    "result.metadata.empty_key",
                    "Metadata keys cannot be null, empty, or whitespace.");

            /// <summary>
            /// Metadata values cannot be null.
            /// </summary>
            /// <param name="key">The metadata key.</param>
            public static Error NullValue(string key) =>
                Error.InternalServerError(
                    "result.metadata.null_value",
                    $"Metadata value for key '{key}' cannot be null.");

            /// <summary>
            /// Metadata exceeds maximum entry count.
            /// </summary>
            /// <param name="maxEntries">The maximum allowed entries.</param>
            public static Error ExceedsMaxEntries(int maxEntries) =>
                Error.InternalServerError(
                    "result.metadata.exceeds_max_entries",
                    $"Metadata cannot contain more than {maxEntries} entries.");
        }

        #endregion

        #region Value

        /// <summary>
        /// Value validation failures.
        /// </summary>
        public static class Value
        {
            /// <summary>
            /// A successful result must have a value.
            /// </summary>
            public static Error Required =>
                Error.InternalServerError(
                    "result.value.required",
                    "A successful result must have a value.");

            /// <summary>
            /// A failed result cannot have a value.
            /// </summary>
            public static Error NotAllowed =>
                Error.InternalServerError(
                    "result.value.not_allowed",
                    "A failed result cannot have a value.");
        }

        #endregion

        #region Status

        /// <summary>
        /// Status code validation failures.
        /// </summary>
        public static class Status
        {
            /// <summary>
            /// Status code outside valid HTTP range.
            /// </summary>
            /// <param name="statusCode">The invalid status code.</param>
            public static Error InvalidStatusCode(int statusCode) =>
                Error.InternalServerError(
                    "result.status.invalid_status_code",
                    $"Status code must be between " +
                    $"{ResultConstant.Constraint.Status.Min} and " +
                    $"{ResultConstant.Constraint.Status.Max}, " +
                    $"but was {statusCode}.");

            /// <summary>
            /// Successful result has invalid status code (not 2xx).
            /// </summary>
            /// <param name="statusCode">The invalid status code.</param>
            public static Error InvalidSuccessStatus(int statusCode) =>
                Error.InternalServerError(
                    "result.status.invalid_success_status",
                    $"A successful result must have a status code between " +
                    $"{ResultConstant.Constraint.Status.Success.Min} and " +
                    $"{ResultConstant.Constraint.Status.Success.Max}, " +
                    $"but was {statusCode}.");

            /// <summary>
            /// Failed result has invalid status code (not 4xx/5xx).
            /// </summary>
            /// <param name="statusCode">The invalid status code.</param>
            public static Error InvalidFailureStatus(int statusCode) =>
                Error.InternalServerError(
                    "result.status.invalid_failure_status",
                    $"A failed result must have a status code between " +
                    $"{ResultConstant.Constraint.Status.Failure.Min} and " +
                    $"{ResultConstant.Constraint.Status.Failure.Max}, " +
                    $"but was {statusCode}.");
        }

        #endregion

        #region Flow

        /// <summary>
        /// Flow control validation failures for async/combinator operations.
        /// </summary>
        public static class Flow
        {
            /// <summary>
            /// Required callback was not provided.
            /// </summary>
            /// <param name="operation">The operation name.</param>
            public static Error CallbackMissing(
                string operation)
                => Error.InternalServerError(
                    "result.flow.callback_missing",
                    $"The required callback for '{operation}' was not provided.");

            /// <summary>
            /// Operation returned a null task.
            /// </summary>
            /// <param name="operation">The operation name.</param>
            public static Error TaskMissing(
                string operation)
                => Error.InternalServerError(
                    "result.flow.task_missing",
                    $"The operation '{operation}' returned a null task.");

            /// <summary>
            /// Operation returned a null result.
            /// </summary>
            /// <param name="operation">The operation name.</param>
            public static Error ResultMissing(
                string operation)
                => Error.InternalServerError(
                    "result.flow.result_missing",
                    $"The operation '{operation}' returned a null result.");

            /// <summary>
            /// Operation returned a null error.
            /// </summary>
            /// <param name="operation">The operation name.</param>
            public static Error ErrorMissing(
                string operation)
                => Error.InternalServerError(
                    "result.flow.error_missing",
                    $"The operation '{operation}' returned a null error.");

            /// <summary>
            /// Exception mapping function was not provided.
            /// </summary>
            /// <param name="operation">The operation name.</param>
            public static Error ExceptionMappingMissing(
                string operation)
                => Error.InternalServerError(
                    "result.flow.exception_mapping_missing",
                    $"The exception mapping for '{operation}' was not provided.");

            /// <summary>
            /// Exception mapping function returned a null error.
            /// </summary>
            /// <param name="operation">The operation name.</param>
            public static Error ExceptionMappingFailed(
                string operation)
                => Error.InternalServerError(
                    "result.flow.exception_mapping_failed",
                    $"The exception mapping for '{operation}' returned a null error.");
        }

        #endregion
    }

    #endregion
}