namespace Core.BuildingBlocks.Domain.ValueObjects.Money;

/// <summary>Groups validation errors for <see cref="Currency"/> creation.</summary>
public static class CurrencyOutcome
{
    /// <summary>Contains currency validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the currency code is missing.</summary>
        public static Error CodeRequired => Error.BadRequest(
            code: Codes.CodeRequired,
            message: "Currency code must be specified.");

        /// <summary>Error returned when the code is not three uppercase letters.</summary>
        public static Error CodeInvalid => Error.BadRequest(
            code: Codes.CodeInvalid,
            message: "Currency code must be a 3-letter ISO-4217 code.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string CodeRequired = "Currency.Code.Required";
        public const string CodeInvalid = "Currency.Code.Invalid";
    }
}