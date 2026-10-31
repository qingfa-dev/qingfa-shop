namespace Core.BuildingBlocks.Functional.Results;

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
                    Codes.Empty,
                    "At least one error is required.");

            /// <summary>
            /// Result exceeds maximum error count.
            /// </summary>
            /// <param name="maxCount">The maximum allowed error count.</param>
            public static Error ExceedsMaxCount(int maxCount) =>
                Error.InternalServerError(
                    Codes.ExceedsMaxCount,
                    $"A result cannot contain more than {maxCount} errors.");

            /// <summary>
            /// All errors in a result must have the same HTTP status code.
            /// </summary>
            public static Error MixedStatusCodes =>
                Error.InternalServerError(
                    Codes.MixedStatusCodes,
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
                    Codes.EmptyKey,
                    "Metadata keys cannot be null, empty, or whitespace.");

            /// <summary>
            /// Metadata values cannot be null.
            /// </summary>
            /// <param name="key">The metadata key.</param>
            public static Error NullValue(string key) =>
                Error.InternalServerError(
                    Codes.NullValue,
                    $"Metadata value for key '{key}' cannot be null.");

            /// <summary>
            /// Metadata exceeds maximum entry count.
            /// </summary>
            /// <param name="maxEntries">The maximum allowed entries.</param>
            public static Error ExceedsMaxEntries(int maxEntries) =>
                Error.InternalServerError(
                    Codes.ExceedsMaxEntries,
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
                    Codes.Required,
                    "A successful result must have a value.");

            /// <summary>
            /// A failed result cannot have a value.
            /// </summary>
            public static Error NotAllowed =>
                Error.InternalServerError(
                    Codes.NotAllowed,
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
                    Codes.InvalidStatusCode,
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
                    Codes.InvalidSuccessStatus,
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
                    Codes.InvalidFailureStatus,
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
                    Codes.CallbackMissing,
                    $"The required callback for '{operation}' was not provided.");

            /// <summary>
            /// Operation returned a null task.
            /// </summary>
            /// <param name="operation">The operation name.</param>
            public static Error TaskMissing(
                string operation)
                => Error.InternalServerError(
                    Codes.TaskMissing,
                    $"The operation '{operation}' returned a null task.");

            /// <summary>
            /// Operation returned a null result.
            /// </summary>
            /// <param name="operation">The operation name.</param>
            public static Error ResultMissing(
                string operation)
                => Error.InternalServerError(
                    Codes.ResultMissing,
                    $"The operation '{operation}' returned a null result.");

            /// <summary>
            /// Operation returned a null error.
            /// </summary>
            /// <param name="operation">The operation name.</param>
            public static Error ErrorMissing(
                string operation)
                => Error.InternalServerError(
                    Codes.ErrorMissing,
                    $"The operation '{operation}' returned a null error.");

            /// <summary>
            /// Exception mapping function was not provided.
            /// </summary>
            /// <param name="operation">The operation name.</param>
            public static Error ExceptionMappingMissing(
                string operation)
                => Error.InternalServerError(
                    Codes.ExceptionMappingMissing,
                    $"The exception mapping for '{operation}' was not provided.");

            /// <summary>
            /// Exception mapping function returned a null error.
            /// </summary>
            /// <param name="operation">The operation name.</param>
            public static Error ExceptionMappingFailed(
                string operation)
                => Error.InternalServerError(
                    Codes.ExceptionMappingFailed,
                    $"The exception mapping for '{operation}' returned a null error.");
        }

        #endregion

        #region Exception

        /// <summary>
        /// An unexpected exception occurred and no factory was supplied.
        /// </summary>
        public static Error UnexpectedException =>
            Error.InternalServerError(
                Codes.UnexpectedException,
                "An unexpected exception occurred.");

        #endregion
    }

    #endregion

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string Empty = "result.errors.empty";
        public const string ExceedsMaxCount = "result.errors.exceeds_max_count";
        public const string MixedStatusCodes = "result.errors.mixed_status_codes";
        public const string EmptyKey = "result.metadata.empty_key";
        public const string NullValue = "result.metadata.null_value";
        public const string ExceedsMaxEntries = "result.metadata.exceeds_max_entries";
        public const string Required = "result.value.required";
        public const string NotAllowed = "result.value.not_allowed";
        public const string InvalidStatusCode = "result.status.invalid_status_code";
        public const string InvalidSuccessStatus = "result.status.invalid_success_status";
        public const string InvalidFailureStatus = "result.status.invalid_failure_status";
        public const string CallbackMissing = "result.flow.callback_missing";
        public const string TaskMissing = "result.flow.task_missing";
        public const string ResultMissing = "result.flow.result_missing";
        public const string ErrorMissing = "result.flow.error_missing";
        public const string ExceptionMappingMissing = "result.flow.exception_mapping_missing";
        public const string ExceptionMappingFailed = "result.flow.exception_mapping_failed";
        public const string UnexpectedException = "result.exception";
    }
}