using Elwark.People.Abstractions;
using Elwark.People.Domain.ErrorCodes;

namespace Elwark.People.Domain.Exceptions
{
    public class ElwarkIdentificationException : ElwarkException
    {
        public ElwarkIdentificationException(IdentificationError code, Identification? identifier = null)
            : base(nameof(IdentificationError), code.ToString("G"))
        {
            Code = code;
            Identifier = identifier;
        }

        public Identification? Identifier { get; }

        public IdentificationError Code { get; }

        public static ElwarkIdentificationException NotFound(Identification? identifier = null) =>
            new ElwarkIdentificationException(IdentificationError.NotFound, identifier);
    }
}