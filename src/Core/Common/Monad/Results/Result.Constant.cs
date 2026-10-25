namespace Core.Common.Monad.Results;

/// <summary>
/// Constants for <see cref="Result"/> constraints, defaults, and HTTP status codes.
/// </summary>
/// <remarks>
/// Centralizes validation limits and default values used by <see cref="Result"/>
/// and <see cref="Result{TValue}"/> to ensure consistent behavior.
/// </remarks>
public static class ResultConstant
{
    #region Constraints

    /// <summary>
    /// Validation constraints for result properties.
    /// </summary>
    public static class Constraint
    {
        /// <summary>
        /// Constraints for the error list.
        /// </summary>
        public static class Errors
        {
            /// <summary>
            /// Maximum number of errors allowed in a single result.
            /// </summary>
            public const int MaxCount = 50;
        }

        /// <summary>
        /// Constraints for the metadata dictionary.
        /// </summary>
        public static class Metadata
        {
            /// <summary>
            /// Maximum number of metadata entries allowed.
            /// </summary>
            public const int MaxEntries = 50;
        }

        /// <summary>
        /// Constraints for the HTTP status code.
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

            /// <summary>
            /// Constraints for successful result status codes (2xx).
            /// </summary>
            public static class Success
            {
                /// <summary>Minimum successful status code (200).</summary>
                public const int Min = 200;

                /// <summary>Maximum successful status code (299).</summary>
                public const int Max = 299;
            }

            /// <summary>
            /// Constraints for failed result status codes (4xx-5xx).
            /// </summary>
            public static class Failure
            {
                /// <summary>Minimum failure status code (400).</summary>
                public const int Min = 400;

                /// <summary>Maximum failure status code (599).</summary>
                public const int Max = 599;
            }
        }
    }

    #endregion

    #region Defaults

    /// <summary>
    /// Default values used when optional parameters are omitted.
    /// </summary>
    public static class Default
    {
        /// <summary>
        /// Default HTTP status code for failed results without errors (500).
        /// </summary>
        public const int FailureStatus = StatusCode.InternalServerError;

        /// <summary>
        /// Empty error list for successful results.
        /// </summary>
        public static readonly List<Error> EmptyErrors = [];
    }

    #endregion

    #region Status Codes

    /// <summary>
    /// Standard HTTP status codes used by the result system.
    /// </summary>
    public static class StatusCode
    {
        #region Success

        /// <summary>HTTP 200 OK.</summary>
        public const int Ok = 200;

        /// <summary>HTTP 201 Created.</summary>
        public const int Created = 201;

        /// <summary>HTTP 202 Accepted.</summary>
        public const int Accepted = 202;

        /// <summary>HTTP 204 No Content.</summary>
        public const int NoContent = 204;

        #endregion

        #region Failure

        /// <summary>HTTP 400 Bad Request.</summary>
        public const int BadRequest = ErrorConstant.StatusCode.BadRequest;

        /// <summary>HTTP 401 Unauthorized.</summary>
        public const int Unauthorized = ErrorConstant.StatusCode.Unauthorized;

        /// <summary>HTTP 403 Forbidden.</summary>
        public const int Forbidden = ErrorConstant.StatusCode.Forbidden;

        /// <summary>HTTP 404 Not Found.</summary>
        public const int NotFound = ErrorConstant.StatusCode.NotFound;

        /// <summary>HTTP 409 Conflict.</summary>
        public const int Conflict = ErrorConstant.StatusCode.Conflict;

        /// <summary>HTTP 422 Unprocessable Entity.</summary>
        public const int UnprocessableEntity =
            ErrorConstant.StatusCode.UnprocessableEntity;

        /// <summary>HTTP 500 Internal Server Error.</summary>
        public const int InternalServerError =
            ErrorConstant.StatusCode.InternalServerError;

        /// <summary>HTTP 503 Service Unavailable.</summary>
        public const int ServiceUnavailable =
            ErrorConstant.StatusCode.ServiceUnavailable;

        #endregion
    }

    #endregion
}