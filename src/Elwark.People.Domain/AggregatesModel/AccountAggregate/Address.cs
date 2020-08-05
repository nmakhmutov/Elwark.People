using System.Collections.Generic;
using Elwark.People.Domain.SeedWork;

namespace Elwark.People.Domain.AggregatesModel.AccountAggregate
{
    public class Address : ValueObject
    {
        public Address(string? countryCode, string? city)
        {
            City = city;
            CountryCode = countryCode;
        }

        public string? CountryCode { get; private set; }
        
        public string? City { get; private set; }

        public override string ToString() =>
            $"{CountryCode} {City}".Trim();

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return CountryCode;
            yield return City;
        }
    }
}