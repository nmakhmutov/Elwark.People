using System;

namespace Elwark.People.Domain.Exceptions
{
    public abstract class ElwarkException : Exception
    {
        protected ElwarkException(string group, string type, string? message)
            : this(group, type, message, null)
        {
        }

        protected ElwarkException(string group, string type, Exception exception)
            : this(group, type, null, exception)
        {
        }

        protected ElwarkException(string group, string type, string? message = null, Exception? exception = null)
            : base(message, exception)
        {
            Group = group;
            Type = type;
        }

        public string Group { get; }

        public string Type { get; }


        public override string ToString() =>
            $"{Group}:{Type}. {Message}";
    }
}