using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Api.Requests
{
    public class SignUpByFacebookRequest : SignUpBaseRequest
    {
        public SignUpByFacebookRequest(Identification.Facebook facebook, string accessToken, Identification.Email email,
            UrlTemplate confirmationUrl)
            : base(email, confirmationUrl)
        {
            Facebook = facebook;
            AccessToken = accessToken;
        }

        public Identification.Facebook Facebook { get; }

        public string AccessToken { get; }
    }
}