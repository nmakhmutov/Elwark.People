using System;

namespace People.Domain.AggregateModels.Account
{
    public sealed record Registration(byte[] Ip, CountryCode CountryCode, DateTime CreatedAt)
    {
        public bool IsEmpty() => Ip.Length == 0 && CountryCode.IsEmpty();
    }
}