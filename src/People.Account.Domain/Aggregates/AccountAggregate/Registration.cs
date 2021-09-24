namespace People.Account.Domain.Aggregates.AccountAggregate
{
    public sealed record Registration(byte[] Ip, CountryCode CountryCode)
    {
        public bool IsEmpty => Ip.Length == 0 && CountryCode.IsEmpty();
    }
}
