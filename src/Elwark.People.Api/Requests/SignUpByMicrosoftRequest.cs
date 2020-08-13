using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Api.Requests
{
    public class SignUpByMicrosoftRequest : SignUpBaseRequest
    {
        public SignUpByMicrosoftRequest(Identification.Microsoft microsoft, string accessToken, Identification.Email email,
            UrlTemplate confirmationUrl)
            : base(email, confirmationUrl)
        {
            Microsoft = microsoft;
            AccessToken = accessToken;
        }

        public Identification.Microsoft Microsoft { get; }

        public string AccessToken { get; }
    }
}