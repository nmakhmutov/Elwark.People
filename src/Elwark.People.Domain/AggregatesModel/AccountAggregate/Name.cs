// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

using System.Collections.Generic;
using Elwark.Extensions;
using Elwark.People.Domain.SeedWork;

namespace Elwark.People.Domain.AggregatesModel.AccountAggregate
{
    public record Name : ValueObject
    {
        public Name(string nickname, string? firstName = null, string? lastName = null)
        {
            Nickname = nickname;
            FirstName = firstName;
            LastName = lastName;
        }

        public string? FirstName { get; private set; }

        public string? LastName { get; private set; }

        public string Nickname { get; private set; }

        public string FullName => string.Join(" ", FirstName ?? string.Empty, LastName ?? string.Empty)
            .Trim()
            .NullIfEmpty() ?? Nickname;

        public override string ToString() =>
            $"{FirstName} {LastName} ({Nickname})".Trim();

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return FirstName;
            yield return LastName;
            yield return Nickname;
        }
    }
}