using System;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace People.Infrastructure.Prohibitions
{
    public class Prohibition
    {
        public Prohibition(ProhibitionType type, string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value cannot be null or empty.", nameof(value));
            
            Type = type;
            Value = value;
        }

        public ProhibitionType Type { get; private set; }
        
        public string Value { get; private set; }
    }
}