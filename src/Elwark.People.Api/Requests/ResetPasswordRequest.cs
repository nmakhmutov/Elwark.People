using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Api.Requests
{
    public class ResetPasswordRequest
    {
        public ResetPasswordRequest(Identification.Email email, UrlTemplate confirmationUrl)
        {
            Email = email;
            ConfirmationUrl = confirmationUrl;
        }

        public Identification.Email Email { get; }

        public UrlTemplate ConfirmationUrl { get; }
    }
}