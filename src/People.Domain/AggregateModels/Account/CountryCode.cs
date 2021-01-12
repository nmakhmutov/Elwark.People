using System;

namespace People.Domain.AggregateModels.Account
{
    public readonly struct CountryCode : IComparable, IComparable<CountryCode>, IEquatable<CountryCode>
    {
        public static CountryCode Empty => new("--");

        private readonly string _value;

        public CountryCode(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value), $"{nameof(CountryCode)} cannot be empty");

            if (value.Length != 2)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    $"{nameof(CountryCode)} value must be two chars");

            _value = value.ToUpperInvariant();
        }

        public bool Equals(CountryCode other) =>
            _value == other._value;

        public override bool Equals(object? obj) =>
            obj is CountryCode other && Equals(other);

        public override int GetHashCode() =>
            _value.GetHashCode();

        public override string ToString() =>
            _value;

        public int CompareTo(CountryCode other) =>
            string.Compare(_value, other._value, StringComparison.Ordinal);

        public int CompareTo(object? obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            return obj is CountryCode other
                ? CompareTo(other)
                : throw new ArgumentException($"Object must be of type {nameof(CountryCode)}");
        }

        public static bool operator <(CountryCode left, CountryCode right) =>
            left.CompareTo(right) < 0;

        public static bool operator >(CountryCode left, CountryCode right) =>
            left.CompareTo(right) > 0;

        public static bool operator <=(CountryCode left, CountryCode right) =>
            left.CompareTo(right) <= 0;

        public static bool operator >=(CountryCode left, CountryCode right) =>
            left.CompareTo(right) >= 0;

        public static bool operator ==(CountryCode left, CountryCode right) =>
            left.Equals(right);

        public static bool operator !=(CountryCode left, CountryCode right) =>
            !left.Equals(right);
    }
}