namespace Elwark.People.Api.Requests
{
    public class RestorePasswordRequest
    {
        public RestorePasswordRequest(string confirmationToken, string password)
        {
            ConfirmationToken = confirmationToken;
            Password = password;
        }

        public string ConfirmationToken { get; }

        public string Password { get; }
    }
}