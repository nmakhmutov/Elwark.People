using Elwark.People.Domain.ErrorCodes;

namespace Elwark.People.Domain.Exceptions
{
    public class ElwarkGoogleException : ElwarkException
    {
        public ElwarkGoogleException(GoogleError code, string? message = null)
            : base(nameof(GoogleError), code.ToString("G"), message) =>
            Code = code;

        public GoogleError Code { get; }
    }
}