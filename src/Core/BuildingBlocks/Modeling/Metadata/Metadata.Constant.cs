namespace Core.BuildingBlocks.Modeling.Metadata;

/// <summary>
/// Standard metadata key names used across the application.
/// </summary>
/// <remarks>
/// Using constant keys ensures consistency and enables compile-time checking
/// when attaching well-known metadata to errors, results, and domain events.
/// </remarks>
public static class MetadataConstant
{
    /// <summary>
    /// Well-known metadata keywords for distributed tracing and correlation.
    /// </summary>
    public static class Keyword
    {
        /// <summary>
        /// Key for the correlation ID that tracks a request across service boundaries.
        /// </summary>
        public const string CorrelationId = nameof(CorrelationId);

        /// <summary>
        /// Key for the causation ID that identifies the event that caused this operation.
        /// </summary>
        public const string CausationId = nameof(CausationId);

        /// <summary>
        /// Key for the request ID that uniquely identifies a single request.
        /// </summary>
        public const string RequestId = nameof(RequestId);

        /// <summary>
        /// Key for the event ID that uniquely identifies a single request.
        /// </summary>
        public const string EventId = nameof(EventId);

        /// <summary>
        /// Key for the timestamp when the metadata was recorded.
        /// </summary>
        public const string Timestamp = nameof(Timestamp);

        /// <summary>
        /// Key for the tenant identifier that scopes the operation.
        /// </summary>
        public const string TenantId = nameof(TenantId);
    }
}