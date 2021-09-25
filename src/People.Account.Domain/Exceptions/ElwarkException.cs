using System;

namespace People.Account.Domain.Exceptions
{
    public class ElwarkException : Exception
    {
        public ElwarkException(string code, string? message = null, Exception? innerException = null)
            : base(message ?? code, innerException) =>
            Code = code;

        public string Code { get; }
    }
}
