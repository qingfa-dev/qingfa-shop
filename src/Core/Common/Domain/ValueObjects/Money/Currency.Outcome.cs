namespace Core.Common.Domain.ValueObjects.Money;

/// <summary>Groups validation errors for <see cref="Currency"/> creation.</summary>
public static class CurrencyOutcome
{
    /// <summary>Contains currency validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the currency code is missing.</summary>
        public static Error CodeRequired => Error.BadRequest(
            code: "Currency.Code.Required",
            message: "Currency code must be specified.");

        /// <summary>Error returned when the code is not three uppercase letters.</summary>
        public static Error CodeInvalid => Error.BadRequest(
            code: "Currency.Code.Invalid",
            message: "Currency code must be a 3-letter ISO-4217 code.");

        #endregion
    }
}