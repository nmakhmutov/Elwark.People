using MediatR;

namespace People.Domain.SeedWork;

public abstract class Entity
{
    private readonly HashSet<INotification> _domainEvents = [];

    public IReadOnlyCollection<INotification> DomainEvents =>
        _domainEvents;

    protected void AddDomainEvent(INotification evt) =>
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
