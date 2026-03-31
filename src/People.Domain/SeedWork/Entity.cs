using People.Domain.Events;

namespace People.Domain.SeedWork;

public abstract class Entity : IHasDomainEvents
{
    private readonly HashSet<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() =>
        _domainEvents.ToArray();

    protected void AddDomainEvent(IDomainEvent evt) =>
        _domainEvents.Add(evt);

    public void ClearDomainEvents() =>
        _domainEvents.Clear();

    public abstract bool IsTransient();
}

public abstract class Entity<T> : Entity,
    IEquatable<Entity<T>> where T : struct
{
    public T Id { get; protected set; }

    public bool Equals(Entity<T>? other)
    {
        if (ReferenceEquals(null, other))
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        return obj.GetType() == GetType() && Equals((Entity<T>)obj);
    }

    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() =>
        397 ^ Id.GetHashCode();

    public static bool operator ==(Entity<T>? left, Entity<T>? right) =>
        Equals(left, right);

    public static bool operator !=(Entity<T>? left, Entity<T>? right) =>
        !Equals(left, right);

    public override bool IsTransient() =>
        Id.Equals(default(T));
}
