using System;

namespace People.Domain.Exceptions;

public class PeopleException : Exception
{
    public PeopleException(string code, string? message = null, Exception? innerException = null)
        : base(message ?? code, innerException) =>
        Code = code;

    public string Code { get; }
}
