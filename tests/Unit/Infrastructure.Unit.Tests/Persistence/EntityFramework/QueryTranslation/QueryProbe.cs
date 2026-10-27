namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.QueryTranslation;

/// <summary>Test entity for query translation tests.</summary>
public sealed record QueryProbe
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Score { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? Tags { get; init; }
    public Priority Priority { get; init; }
}

public enum Priority
{
    Low,
    Medium,
    High
}