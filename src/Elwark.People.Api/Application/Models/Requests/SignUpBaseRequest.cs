using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Api.Application.Models.Requests
{
    public abstract class SignUpBaseRequest
    {
        protected SignUpBaseRequest(Identification.Email email, UrlTemplate confirmationUrl)
        {
            Email = email;
            ConfirmationUrl = confirmationUrl;
        }

        public Identification.Email Email { get; }

        public UrlTemplate ConfirmationUrl { get; }
    }
}