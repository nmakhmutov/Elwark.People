using System.Collections.Generic;
using Elwark.People.Domain.SeedWork;

namespace Elwark.People.Domain.AggregatesModel.AccountAggregate
{
    public sealed record Address(string? CountryCode, string? City) : ValueObject
    {
        public override string ToString() =>
            $"{CountryCode} {City}".Trim();

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return CountryCode;
            yield return City;
        }
    }
}