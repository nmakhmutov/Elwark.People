using Elwark.People.Domain.ErrorCodes;

namespace Elwark.People.Domain.Exceptions
{
    public class ElwarkMicrosoftException : ElwarkException
    {
        public ElwarkMicrosoftException(MicrosoftError code, string? message = null)
            : base(nameof(MicrosoftError), code.ToString("G"), message) =>
            Code = code;

        public MicrosoftError Code { get; }
    }
}