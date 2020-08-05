using System;
using Elwark.People.Domain.ErrorCodes;

namespace Elwark.People.Domain.Exceptions
{
    public class ElwarkCryptographyException : ElwarkException
    {
        public ElwarkCryptographyException(CryptographyError code, Exception exception)
            : base(nameof(CryptographyError), code.ToString("G"), exception) =>
            Code = code;

        public CryptographyError Code { get; }
    }
}