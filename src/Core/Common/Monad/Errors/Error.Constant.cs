namespace Core.Common.Monad.Errors;

/// <summary>
/// Centralized constants for <see cref="Error"/> constraints, metadata keys,
/// HTTP status codes, and failure representations.
/// </summary>
/// <remarks>
/// Keeping constraints and error representations in a single location ensures
/// consistent validation and error messages across the codebase.
/// </remarks>
public static class ErrorConstant
{
    #region Constraints

    /// <summary>
    /// Length and range constraints for error members.
    /// </summary>
    public static class Constraint
    {
        /// <summary>
        /// Constraints for the <see cref="Error.Code"/> property.
        /// </summary>
        public static class Code
        {
            /// <summary>
            /// Maximum allowed length for an error code.
            /// </summary>
            public const int MaxLength = 256;
        }

        /// <summary>
        /// Constraints for the <see cref="Error.Message"/> property.
        /// </summary>
        public static class Message
        {
            /// <summary>
            /// Maximum allowed length for an error message.
            /// </summary>
            public const int MaxLength = 1024;
        }

        /// <summary>
        /// Constraints for the <see cref="Error.Type"/> property.
        /// </summary>
        public static class Type
        {
            /// <summary>
            /// Maximum allowed length for an error type.
            /// </summary>
            public const int MaxLength = 256;
        }

        /// <summary>
        /// Constraints for the <see cref="Error.Status"/> property.
        /// </summary>
        public static class Status
        {
            /// <summary>
            /// Minimum valid HTTP status code.
            /// </summary>
            public const int Min = 100;

            /// <summary>
            /// Maximum valid HTTP status code.
            /// </summary>
            public const int Max = 599;
        }

        /// <summary>
        /// Constraints for the <see cref="Error.Instance"/> property.
        /// </summary>
        public static class Instance
        {
            /// <summary>
            /// Maximum allowed length for an instance identifier.
            /// </summary>
            public const int MaxLength = 256;
        }
    }

    #endregion

    #region Metadata

    /// <summary>
    /// Standard metadata keys used across errors and results.
    /// </summary>
    public static class Metadata
    {
        /// <summary>
        /// Key for the distributed trace identifier.
        /// </summary>
        public const string TraceId = nameof(TraceId);

        /// <summary>
        /// Key for the error timestamp.
        /// </summary>
        public const string Timestamp = nameof(Timestamp);

        /// <summary>
        /// Key for the affected resource identifier.
        /// </summary>
        public const string Resource = nameof(Resource);

        /// <summary>
        /// Key for the field identifier (e.g., validation field).
        /// </summary>
        public const string Field = nameof(Field);

        /// <summary>
        /// Key for the retry attempt number.
        /// </summary>
        public const string Attempt = nameof(Attempt);

        /// <summary>
        /// Key for the type of the provider exception behind a failure.
        /// </summary>
        public const string ExceptionType = nameof(ExceptionType);

        /// <summary>
        /// Key for the text of the provider exception behind a failure.
        /// </summary>
        public const string ExceptionMessage = nameof(ExceptionMessage);
    }

    #endregion

    #region Status Codes

    /// <summary>
    /// Standard HTTP status codes used by the error system.
    /// </summary>
    public static class StatusCode
    {
        #region Success

        /// <summary>HTTP 200 OK.</summary>
        public const int Ok = 200;

        /// <summary>HTTP 201 Created.</summary>
        public const int Created = 201;

        #endregion

        #region Failure

        /// <summary>HTTP 400 Bad Request.</summary>
        public const int BadRequest = 400;

        /// <summary>HTTP 401 Unauthorized.</summary>
        public const int Unauthorized = 401;

        /// <summary>HTTP 403 Forbidden.</summary>
        public const int Forbidden = 403;

        /// <summary>HTTP 404 Not Found.</summary>
        public const int NotFound = 404;

        /// <summary>HTTP 409 Conflict.</summary>
        public const int Conflict = 409;

        /// <summary>HTTP 422 Unprocessable Entity.</summary>
        public const int UnprocessableEntity = 422;

        /// <summary>HTTP 500 Internal Server Error.</summary>
        public const int InternalServerError = 500;

        /// <summary>HTTP 501 Not Implemented.</summary>
        public const int NotImplemented = 501;

        /// <summary>HTTP 502 Bad Gateway.</summary>
        public const int BadGateway = 502;

        /// <summary>HTTP 503 Service Unavailable.</summary>
        public const int ServiceUnavailable = 503;

        /// <summary>HTTP 504 Gateway Timeout.</summary>
        public const int GatewayTimeout = 504;

        #endregion
    }

    #endregion

    #region Result

    /// <summary>
    /// Predefined error representations used by <see cref="Result"/> validation.
    /// </summary>
    public static class Result
    {
        /// <summary>
        /// Failure error representations.
        /// </summary>
        public static class Failure
        {
            /// <summary>
            /// Status code validation failures.
            /// </summary>
            public static class Status
            {
                /// <summary>
                /// Status code out of valid range.
                /// </summary>
                public static class OutOfRange
                {
                    /// <summary>Error code: <c>error.status.out_of_range</c></summary>
                    public const string Code =
                        "error.status.out_of_range";

                    /// <summary>Error message template.</summary>
                    public const string Message =
                        "The status code is out of the valid range ({0} - {1}).";

                    /// <summary>Combined code:message representation.</summary>
                    public const string Representation =
                        Code + ":" + Message;
                }
            }

            /// <summary>
            /// Error code validation failures.
            /// </summary>
            public static class Code
            {
                /// <summary>
                /// Null or whitespace error code.
                /// </summary>
                public static class NullOrWhitespace
                {
                    /// <summary>Error code: <c>error.code.null_or_whitespace</c></summary>
                    public const string Code =
                        "error.code.null_or_whitespace";

                    /// <summary>Error message.</summary>
                    public const string Message =
                        "The error code cannot be null or whitespace.";

                    /// <summary>Combined code:message representation.</summary>
                    public const string Representation =
                        Code + ":" + Message;
                }

                /// <summary>
                /// Error code exceeds maximum length.
                /// </summary>
                public static class ExceedsMaxLength
                {
                    /// <summary>Error code: <c>error.code.exceeds_max_length</c></summary>
                    public const string Code =
                        "error.code.exceeds_max_length";

                    /// <summary>Error message template.</summary>
                    public const string Message =
                        "The error code exceeds the maximum length of '{0}' characters.";

                    /// <summary>Combined code:message representation.</summary>
                    public const string Representation =
                        Code + ":" + Message;
                }
            }

            /// <summary>
            /// Error message validation failures.
            /// </summary>
            public static class Message
            {
                /// <summary>
                /// Null or whitespace error message.
                /// </summary>
                public static class NullOrWhitespace
                {
                    /// <summary>Error code: <c>error.message.null_or_whitespace</c></summary>
                    public const string Code =
                        "error.message.null_or_whitespace";

                    /// <summary>Error message.</summary>
                    public const string Message =
                        "The error message cannot be null or whitespace.";

                    /// <summary>Combined code:message representation.</summary>
                    public const string Representation =
                        Code + ":" + Message;
                }

                /// <summary>
                /// Error message exceeds maximum length.
                /// </summary>
                public static class ExceedsMaxLength
                {
                    /// <summary>Error code: <c>error.message.exceeds_max_length</c></summary>
                    public const string Code =
                        "error.message.exceeds_max_length";

                    /// <summary>Error message template.</summary>
                    public const string Message =
                        "The error message exceeds the maximum length of '{0}' characters.";

                    /// <summary>Combined code:message representation.</summary>
                    public const string Representation =
                        Code + ":" + Message;
                }
            }

            /// <summary>
            /// Error type validation failures.
            /// </summary>
            public static class Type
            {
                /// <summary>
                /// Error type exceeds maximum length.
                /// </summary>
                public static class ExceedsMaxLength
                {
                    /// <summary>Error code: <c>error.type.exceeds_max_length</c></summary>
                    public const string Code =
                        "error.type.exceeds_max_length";

                    /// <summary>Error message template.</summary>
                    public const string Message =
                        "The error type exceeds the maximum length of '{0}' characters.";

                    /// <summary>Combined code:message representation.</summary>
                    public const string Representation =
                        Code + ":" + Message;
                }
            }

            /// <summary>
            /// Instance identifier validation failures.
            /// </summary>
            public static class Instance
            {
                /// <summary>
                /// Instance identifier exceeds maximum length.
                /// </summary>
                public static class ExceedsMaxLength
                {
                    /// <summary>Error code: <c>error.instance.exceeds_max_length</c></summary>
                    public const string Code =
                        "error.instance.exceeds_max_length";

                    /// <summary>Error message template.</summary>
                    public const string Message =
                        "The instance exceeds the maximum length of '{0}' characters.";

                    /// <summary>Combined code:message representation.</summary>
                    public const string Representation =
                        Code + ":" + Message;
                }
            }
        }
    }

    #endregion
}