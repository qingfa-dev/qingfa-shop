namespace Core.BuildingBlocks.Modeling.Metadata;

/// <summary>
/// Predefined error representations for metadata operations.
/// </summary>
/// <remarks>
/// Centralizes error codes and messages used by metadata validation
/// to ensure consistent error handling across the codebase.
/// </remarks>
public static class MetadataOutcome
{
    /// <summary>Stable error codes used by the metadata factories.</summary>
    public static class Codes
    {
        /// <summary>Metadata key not found.</summary>
        public const string KeyMetadataNotFound = "key.metadata.not_found";

        /// <summary>Metadata value has the wrong type.</summary>
        public const string ValueInvalidType = "value.metadata.invalid_type";
    }

    public static class Success
    {
        // TODO: expand this in the future
    }

    public static class Failure
    {
        public static class Request
        {
            public static class Argument
            {
                public static class Null
                {
                    /// <summary>Error code: <c>request.argument.null</c></summary>
                    public const string Code = "request.argument.null";
                    /// <summary>Error message.</summary>
                    public const string Message = "The request argument must not be null.";
                    /// <summary>Combined code:message representation.</summary>
                    public const string Representation = Code + ":" + Message;
                }
            }
        }

        public static class Key
        {
            public static class Argument
            {
                public static class Null
                {
                    /// <summary>Error code: <c>key.argument.null</c></summary>
                    public const string Code = "key.argument.null";
                    /// <summary>Error message.</summary>
                    public const string Message = "The key argument must not be null.";
                    /// <summary>Combined code:message representation.</summary>
                    public const string Representation = Code + ":" + Message;
                }
            }

            public static class Metadata
            {
                public static class NotFound
                {
                    /// <summary>Error code: <c>key.metadata.not_found</c></summary>
                    public const string Code = Codes.KeyMetadataNotFound;
                    /// <summary>Error message.</summary>
                    public const string Message = "The specified metadata key was not found.";
                    /// <summary>Combined code:message representation.</summary>
                    public const string Representation = Code + ":" + Message;

                    /// <summary>Creates the not-found error for a missing key.</summary>
                    public static Error ToError() => Error.NotFound(Codes.KeyMetadataNotFound, Message);
                }
            }
        }

        public static class Value
        {
            public static class Metadata
            {
                public static class InvalidType
                {
                    /// <summary>Error code: <c>value.metadata.invalid_type</c></summary>
                    public const string Code = Codes.ValueInvalidType;
                    /// <summary>Error message.</summary>
                    public const string Message = "The metadata value is not of the requested type.";
                    /// <summary>Combined code:message representation.</summary>
                    public const string Representation = Code + ":" + Message;

                    /// <summary>Creates the invalid-type error for a metadata value.</summary>
                    public static Error ToError() => Error.Validation(Codes.ValueInvalidType, Message);
                }
            }
        }

        public static class Dictionary
        {
            public static class Argument
            {
                public static class Null
                {
                    /// <summary>Error code: <c>dictionary.argument.null</c></summary>
                    public const string Code = "dictionary.argument.null";
                    /// <summary>Error message.</summary>
                    public const string Message = "The source dictionary must not be null.";
                    /// <summary>Combined code:message representation.</summary>
                    public const string Representation = Code + ":" + Message;
                }
            }

            public static class Mutation
            {
                public static class NotSupported
                {
                    /// <summary>Error code: <c>dictionary.mutation.not_supported</c></summary>
                    public const string Code = "dictionary.mutation.not_supported";
                    /// <summary>Error message.</summary>
                    public const string Message =
                        "The underlying metadata dictionary does not support mutation.";
                    /// <summary>Combined code:message representation.</summary>
                    public const string Representation = Code + ":" + Message;
                }
            }
        }
    }
}