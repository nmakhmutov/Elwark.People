using System;
using MongoDB.Bson;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace People.Account.Infrastructure.Forbidden
{
    public sealed record ForbiddenItem
    {
        public ForbiddenItem(ForbiddenType type, string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value cannot be null or empty.", nameof(value));
            
            Id = ObjectId.Empty;
            Type = type;
            Value = value;
        }

        public ObjectId Id { get; private set; }
        
        public ForbiddenType Type { get; private set; }
        
        public string Value { get; private set; }
    }
}