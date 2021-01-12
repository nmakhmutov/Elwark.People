using System;

namespace People.Domain.AggregateModels.Account
{
    public sealed record Registration(byte[] IpHash, CountryCode CountryCode, DateTime CreatedAt);
}