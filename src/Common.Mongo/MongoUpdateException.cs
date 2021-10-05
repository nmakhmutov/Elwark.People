using MongoDB.Driver;

namespace Common.Mongo;

public sealed class MongoUpdateException : MongoException
{
    public MongoUpdateException(string message)
        : base(message)
    {
    }
}
