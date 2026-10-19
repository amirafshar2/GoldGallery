namespace Gold.Domain.Entities;

/// <summary>Base for aggregates: Guid id (public, IDOR-safe), audit fields, soft delete.</summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; protected set; }

    public void SoftDelete() => IsDeleted = true;
}

/// <summary>
/// Optimistic concurrency token that works on both SQLite and SQL Server.
/// Must be regenerated on every state change of sellable/payable entities.
/// </summary>
public interface IConcurrencyStamped
{
    Guid ConcurrencyStamp { get; }
}
