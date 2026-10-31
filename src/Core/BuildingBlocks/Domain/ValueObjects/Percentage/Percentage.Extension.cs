namespace Core.BuildingBlocks.Domain.ValueObjects.Percentage;

/// <summary>Provides conversions and calculations for percentages.</summary>
public static class PercentageExtensions
{
    /// <summary>
    /// Converts the percentage to a decimal fraction (50% -> 0.5m).
    /// </summary>
    /// <summary>Converts a percentage number to its decimal fraction.</summary>
    /// <param name="percentage">The percentage to convert.</param>
    /// <returns>The percentage divided by one hundred.</returns>
    public static decimal ToFraction(this Percentage percentage)
    {
        return percentage.Value / PercentageConstant.Defaults.Hundred;
    }

    /// <summary>
    /// Applies the percentage to an amount (10% of 200 -> 20).
    /// </summary>
    /// <summary>Calculates a percentage of an amount.</summary>
    /// <param name="percentage">The percentage to apply.</param>
    /// <param name="amount">The base amount.</param>
    /// <returns>The amount multiplied by the percentage fraction.</returns>
    public static decimal ApplyTo(
        this Percentage percentage,
        decimal amount)
    {
        return amount * percentage.ToFraction();
    }
}