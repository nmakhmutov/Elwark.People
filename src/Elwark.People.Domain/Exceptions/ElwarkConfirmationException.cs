using System;
using Elwark.People.Domain.ErrorCodes;

namespace Elwark.People.Domain.Exceptions
{
    public class ElwarkConfirmationException : ElwarkException
    {
        public ElwarkConfirmationException(ConfirmationError code)
            : base(nameof(ConfirmationError), code.ToString("G")) =>
            Code = code;

        public ElwarkConfirmationException(ConfirmationError code, DateTimeOffset retryAfter)
            : base(nameof(ConfirmationError), code.ToString("G"))
        {
            Code = code;
            RetryAfter = retryAfter;
        }

        public DateTimeOffset? RetryAfter { get; }

        public ConfirmationError Code { get; }
    }
}