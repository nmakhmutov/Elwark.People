using Elwark.People.Domain.ErrorCodes;

namespace Elwark.People.Domain.Exceptions
{
    public class ElwarkPasswordException : ElwarkException
    {
        public ElwarkPasswordException(PasswordError code)
            : base(nameof(PasswordError), code.ToString("G")) =>
            Code = code;

        public PasswordError Code { get; }
    }
}