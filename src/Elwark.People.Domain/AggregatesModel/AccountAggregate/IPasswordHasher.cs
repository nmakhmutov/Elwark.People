namespace Elwark.People.Domain.AggregatesModel.AccountAggregate
{
    public interface IPasswordHasher
    {
        byte[] CreateSalt();

        byte[] CreatePasswordHash(string password, byte[] salt);

        bool IsEqual(string password, byte[] passwordHash, byte[] salt);
    }
}