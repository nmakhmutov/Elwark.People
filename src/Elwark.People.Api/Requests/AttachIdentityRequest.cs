namespace Elwark.People.Api.Application.Models.Requests
{
    public class AttachIdentityRequest
    {
        public AttachIdentityRequest(string accessToken) =>
            AccessToken = accessToken;

        public string AccessToken { get; }
    }
}