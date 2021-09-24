using MongoDB.Driver;

namespace People.Mongo
{
    public sealed class MongoUpdateException : MongoException
    {
        public MongoUpdateException(string message)
            : base(message)
        {
        }
    }
}
