using MongoDB.Driver;

namespace People.Infrastructure.Mongo
{
    public class MongoUpdateException : MongoException
    {
        public MongoUpdateException(string message)
            : base(message, null)
        {
        }
    }
}