using System;

namespace People.Domain.AggregateModels.Account
{
    public sealed record Registration(string Ip, CountryCode CountryCode, DateTime CreatedAt);
}