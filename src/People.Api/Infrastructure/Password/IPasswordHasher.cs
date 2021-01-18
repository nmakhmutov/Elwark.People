namespace People.Api.Infrastructure.Password
{
    public interface IPasswordHasher
    {
        byte[] CreateSalt();

        byte[] CreateHash(string password, byte[] salt);
    }
}