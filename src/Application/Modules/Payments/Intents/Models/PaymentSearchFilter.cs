namespace Application.Modules.Payments.Intents.Models;

/// <summary>Admin payment search filters.</summary>
public sealed record PaymentSearchFilter(
    string? Status,
    string? Provider,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo);
