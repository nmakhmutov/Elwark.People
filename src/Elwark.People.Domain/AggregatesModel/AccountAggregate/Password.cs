using System;
using System.Collections.Generic;
using Elwark.People.Domain.SeedWork;

namespace Elwark.People.Domain.AggregatesModel.AccountAggregate
{
    public record Password : ValueObject
    {
        private readonly DateTimeOffset _createdAt;

        private Password()
        {
            _createdAt = DateTimeOffset.UtcNow;
            Hash = Array.Empty<byte>();
            Salt = Array.Empty<byte>();
        }

        public Password(byte[] hash, byte[] salt)
            : this()
        {
            Hash = hash ?? throw new ArgumentNullException(nameof(hash));
            Salt = salt ?? throw new ArgumentNullException(nameof(salt));

            if (hash.Length == 0)
                throw new ArgumentException(@"Value cannot be an empty collection.", nameof(hash));
            if (salt.Length == 0)
                throw new ArgumentException(@"Value cannot be an empty collection.", nameof(salt));
        }

        public byte[] Hash { get; private set; }
        
        public byte[] Salt { get; private set; }
        
        public DateTime CreatedAt => _createdAt.UtcDateTime;

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Hash;
            yield return Salt;
            yield return _createdAt;
        }
    }
}