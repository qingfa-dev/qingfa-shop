using Core.Common.Domain.Entities;

namespace Infrastructure.Integration.Tests.Persistence;

/// <summary>Simple test entity for EF Core integration tests.</summary>
public class TestProduct : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
