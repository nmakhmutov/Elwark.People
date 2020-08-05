using Elwark.People.Abstractions;

namespace Elwark.People.Api.Application.Models.Requests
{
    public class SignInRequest
    {
        public SignInRequest(Identification identification, string verifier)
        {
            Identification = identification;
            Verifier = verifier;
        }

        public Identification Identification { get; }

        public string Verifier { get; }
    }
}