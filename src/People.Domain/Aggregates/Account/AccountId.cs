using System;

namespace People.Domain.Aggregates.Account
{
    public readonly struct AccountId : IComparable, IComparable<AccountId>, IEquatable<AccountId>
    {
        private readonly long _value;

        public AccountId(long value) =>
            _value = value;

        public int CompareTo(AccountId other) => _value.CompareTo(other._value);

        public int CompareTo(object? obj) =>
            obj switch
            {
                null => 1,
                AccountId code => CompareTo(code),
                _ => throw new ArgumentException($"Object is not {nameof(AccountId)}")
            };

        public bool Equals(AccountId other) => _value == other._value;

        public override bool Equals(object? obj) => obj is AccountId other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => _value.ToString();

        public static bool operator ==(AccountId a, AccountId b) => a.CompareTo(b) == 0;
        public static bool operator !=(AccountId a, AccountId b) => !(a == b);

        public static bool operator >(AccountId a, AccountId b) => a._value > b._value;
        public static bool operator <(AccountId a, AccountId b) => a._value < b._value;

        public static bool operator <=(AccountId a, AccountId b) => a._value <= b._value;
        public static bool operator >=(AccountId a, AccountId b) => a._value >= b._value;

        public static explicit operator long(AccountId id) => id._value;
        public static implicit operator AccountId(long id) => new(id);

        public static AccountId Parse(string? value) =>
            long.Parse(value ?? throw new ArgumentNullException(nameof(value)));

        public static bool TryParse(string? value, out AccountId id)
        {
            if (long.TryParse(value, out var result))
            {
                id = result;
                return true;
            }

            id = new AccountId(default);
            return false;
        }
    }
}