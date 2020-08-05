using Elwark.People.Domain.ErrorCodes;

namespace Elwark.People.Domain.Exceptions
{
    public class ElwarkFacebookException : ElwarkException
    {
        public ElwarkFacebookException(FacebookError code, string? message = null)
            : base(nameof(FacebookError), code.ToString("G"), message) =>
            Code = code;

        public FacebookError Code { get; }
    }
}