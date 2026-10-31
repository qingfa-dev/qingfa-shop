namespace Application.BuildingBlocks.Persistence.Initialization.Resetters;
/// <summary>Resetter validation failures.</summary>
public static class ResetterOutcome
{
    public static class Failure
    {
        public static Error OrderNegative(string parameterName = "order") => Error.Validation(
            Codes.OrderNegative, $"{parameterName} cannot be negative.");

        public static Error CountNegative(string parameterName = "count") => Error.Validation(
            Codes.CountNegative, $"{parameterName} cannot be negative.");

        public static Error CountExceedsMax(string parameterName = "count") => Error.Validation(
            Codes.CountExceedsMax,
            $"{parameterName} cannot exceed {ResetterConstant.Limits.MaxResetters} resetters.");

        public static Error ScopeNull(string parameterName = "scope") => Error.Validation(
            Codes.ScopeNull, $"{parameterName} cannot be null.");

        public static Error ScopeInvalid(string parameterName = "scope") => Error.Validation(
            Codes.ScopeInvalid,
            $"{parameterName} must be one of: {ResetterConstant.Scope.Data}, {ResetterConstant.Scope.Schema}, {ResetterConstant.Scope.All}.");
    }

    /// <summary>Stable codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string OrderNegative    = "Resetter.Order.Negative";
        public const string CountNegative    = "Resetter.Count.Negative";
        public const string CountExceedsMax  = "Resetter.Count.ExceedsMax";
        public const string ScopeNull        = "Resetter.Scope.Null";
        public const string ScopeInvalid     = "Resetter.Scope.Invalid";
    }
}