namespace Core.Common.Domain.ValueObjects.Money;

/// <summary>Groups validation and arithmetic errors for <see cref="Money"/>.</summary>
public static class MoneyOutcome
{
    /// <summary>Contains money validation and operation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the currency code is invalid.</summary>
        public static Error CurrencyInvalid => Error.BadRequest(
            code: "Money.Currency.Invalid",
            message: "Money currency must be a valid ISO-4217 code.");

        /// <summary>Error returned when arithmetic operands use different currencies.</summary>
        public static Error CurrencyMismatch => Error.BadRequest(
            code: "Money.Currency.Mismatch",
            message: "Currencies must match for this operation.");

        #endregion
    }
}