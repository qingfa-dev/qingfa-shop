namespace Core.BuildingBlocks.Domain.ValueObjects.Money;

/// <summary>Groups validation and arithmetic errors for <see cref="Money"/>.</summary>
public static class MoneyOutcome
{
    /// <summary>Contains money validation and operation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the currency code is invalid.</summary>
        public static Error CurrencyInvalid => Error.BadRequest(
            code: Codes.CurrencyInvalid,
            message: "Money currency must be a valid ISO-4217 code.");

        /// <summary>Error returned when arithmetic operands use different currencies.</summary>
        public static Error CurrencyMismatch => Error.BadRequest(
            code: Codes.CurrencyMismatch,
            message: "Currencies must match for this operation.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string CurrencyInvalid = "Money.Currency.Invalid";
        public const string CurrencyMismatch = "Money.Currency.Mismatch";
    }
}