using Elwark.People.Domain.ErrorCodes;

namespace Elwark.People.Domain.Exceptions
{
    public class ElwarkConfirmationException : ElwarkException
    {
        public ElwarkConfirmationException(ConfirmationError code)
            : base(nameof(ConfirmationError), code.ToString("G")) =>
            Code = code;

        public ConfirmationError Code { get; }
    }
}