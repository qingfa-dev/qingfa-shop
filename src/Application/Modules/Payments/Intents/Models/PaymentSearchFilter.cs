namespace Application.Modules.Payments.Intents.Models;

/// <summary>Admin payment search filters.</summary>
/// <param name="Status">An optional lifecycle status.</param>
/// <param name="Provider">An optional provider name.</param>
/// <param name="Currency">An optional ISO 4217 currency code.</param>
/// <param name="CreatedFrom">An optional inclusive lower bound.</param>
/// <param name="CreatedTo">An optional inclusive upper bound.</param>
public sealed record PaymentSearchFilter(
    string? Status,
    string? Provider,
    string? Currency,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo);
