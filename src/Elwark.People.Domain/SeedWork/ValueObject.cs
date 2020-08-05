using System;
using System.Collections.Generic;
using System.Linq;

namespace Elwark.People.Domain.SeedWork
{
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        public bool Equals(ValueObject? other)
        {
            if (other is null)
                return false;

            using var thisValues = GetAtomicValues().GetEnumerator();
            using var otherValues = other.GetAtomicValues().GetEnumerator();

            while (thisValues.MoveNext() && otherValues.MoveNext())
            {
                if (ReferenceEquals(thisValues.Current, null) ^ ReferenceEquals(otherValues.Current, null))
                    return false;

                if (thisValues.Current is { } &&
                    !thisValues.Current.Equals(otherValues.Current))
                    return false;
            }

            return !thisValues.MoveNext() && !otherValues.MoveNext();
        }

        protected abstract IEnumerable<object?> GetAtomicValues();

        public override bool Equals(object? obj)
        {
            if (obj is null || obj.GetType() != GetType())
                return false;

            return Equals((ValueObject) obj);
        }

        public static bool operator ==(ValueObject? left, ValueObject? right) =>
            Equals(left, right);

        public static bool operator !=(ValueObject? left, ValueObject? right) =>
            !Equals(left, right);

        public override int GetHashCode() =>
            GetAtomicValues()
                .Select(x => x is null ? 0 : x.GetHashCode())
                .Aggregate((x, y) => x ^ y);
    }
}