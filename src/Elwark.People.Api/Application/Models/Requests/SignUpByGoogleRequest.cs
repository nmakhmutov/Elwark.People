using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Api.Application.Models.Requests
{
    public class SignUpByGoogleRequest : SignUpBaseRequest
    {
        public SignUpByGoogleRequest(Identification.Google google, string accessToken, Identification.Email email,
            UrlTemplate confirmationUrl)
            : base(email, confirmationUrl)
        {
            Google = google;
            AccessToken = accessToken;
        }

        public Identification.Google Google { get; }

        public string AccessToken { get; }
    }
}