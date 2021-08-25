namespace People.Domain.Aggregates.AccountAggregate
{
    public interface IPasswordHasher
    {
        byte[] CreateSalt();

        byte[] CreateHash(string password, byte[] salt);
    }
}
