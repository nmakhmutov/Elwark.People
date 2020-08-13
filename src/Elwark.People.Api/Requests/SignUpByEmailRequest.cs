using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Api.Requests
{
    public class SignUpByEmailRequest : SignUpBaseRequest
    {
        public SignUpByEmailRequest(Identification.Email email, string password, UrlTemplate confirmationUrl)
            : base(email, confirmationUrl)
        {
            Password = password;
        }

        public string Password { get; }
    }
}