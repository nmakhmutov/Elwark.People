// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

using MongoDB.Bson;

namespace People.Account.Infrastructure.Sequences
{
    public sealed class Sequence
    {
        public Sequence(string name)
        {
            Id = ObjectId.Empty;
            Name = name;
            Value = 1;
        }

        public ObjectId Id { get; private set; }
        
        public string Name { get; private set; }

        public long Value { get; private set; }
    }
}