namespace Elwark.People.Api.Infrastructure.Security
{
    public interface IDataEncryption
    {
        string EncryptToString<T>(T model);

        T DecryptFromString<T>(string cipher);

        byte[] EncryptToBytes<T>(T model);

        T DecryptFromBytes<T>(byte[] cipher);
    }
}