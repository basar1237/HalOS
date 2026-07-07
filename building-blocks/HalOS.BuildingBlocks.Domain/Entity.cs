namespace HalOS.BuildingBlocks.Domain;

/// <summary>
/// Base class for domain entities with a typed identity. Equality is based on the
/// entity's identity (two entities are equal when they share the same non-default Id
/// and concrete type), not on their attribute values.
/// </summary>
/// <typeparam name="TId">Identity type (e.g. <see cref="Guid"/>).</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>Parameterless constructor for ORM materialization only.</summary>
    protected Entity()
    {
        Id = default!;
    }

    public TId Id { get; protected set; }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        // Transient entities (default Id) are never equal to one another.
        if (EqualityComparer<TId>.Default.Equals(Id, default!))
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    public override int GetHashCode() =>
        EqualityComparer<TId>.Default.GetHashCode(Id) * 41;

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !Equals(left, right);
}
