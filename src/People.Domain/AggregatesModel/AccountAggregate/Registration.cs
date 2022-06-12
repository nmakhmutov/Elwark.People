using People.Domain.SeedWork;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace People.Domain.AggregatesModel.AccountAggregate;

public sealed class Registration : ValueObject
{
    public Registration(byte[] ip, CountryCode countryCode)
    {
        Ip = ip;
        CountryCode = countryCode;
    }

    public byte[] Ip { get; private set; }

    public CountryCode CountryCode { get; private set; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var b in Ip)
            yield return b;

        yield return CountryCode;
    }
}
