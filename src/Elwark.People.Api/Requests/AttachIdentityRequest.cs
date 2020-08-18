namespace Elwark.People.Api.Requests
{
    public class AttachIdentityRequest
    {
        public AttachIdentityRequest(string accessToken) =>
            AccessToken = accessToken;

        public string AccessToken { get; }
    }
}