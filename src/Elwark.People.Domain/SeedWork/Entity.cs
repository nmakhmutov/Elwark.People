using System;
using System.Collections.Generic;
using MediatR;

namespace Elwark.People.Domain.SeedWork
{
    public abstract class Entity
    {
        private readonly List<INotification> _domainEvents;

        protected Entity() =>
            _domainEvents = new List<INotification>();

        public IReadOnlyCollection<INotification> DomainEvents =>
            _domainEvents.AsReadOnly();

        protected void AddDomainEvent(INotification evt) =>
            _domainEvents.Add(evt);

        protected void RemoveDomainEvent(INotification evt) =>
            _domainEvents.Remove(evt);

        public void ClearDomainEvents() =>
            _domainEvents.Clear();

        public abstract bool IsTransient();
    }

    public abstract class Entity<T> : Entity, IEquatable<Entity<T>> where T : struct
    {
        public T Id { get; protected set; }

        public bool Equals(Entity<T>? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Entity<T>) obj);
        }

        public override int GetHashCode() =>
            397 ^ Id.GetHashCode();

        public static bool operator ==(Entity<T>? left, Entity<T>? right) =>
            Equals(left, right);

        public static bool operator !=(Entity<T>? left, Entity<T>? right) =>
            !Equals(left, right);

        public override bool IsTransient() =>
            Id.Equals(default(T));
    }
}