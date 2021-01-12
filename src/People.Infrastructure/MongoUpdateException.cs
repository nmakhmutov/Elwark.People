using MongoDB.Driver;

namespace People.Infrastructure
{
    public class MongoUpdateException : MongoException
    {
        public MongoUpdateException(string message)
            : base(message, null)
        {
        }
    }
}