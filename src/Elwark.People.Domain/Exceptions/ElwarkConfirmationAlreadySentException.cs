using System;
using Elwark.People.Domain.ErrorCodes;

namespace Elwark.People.Domain.Exceptions
{
    public class ElwarkConfirmationAlreadySentException : ElwarkConfirmationException
    {
        public ElwarkConfirmationAlreadySentException(DateTimeOffset retryAfter)
            : base(ConfirmationError.AlreadySent)
        {
            RetryAfter = retryAfter;
        }

        public DateTimeOffset RetryAfter { get; }
    }
}