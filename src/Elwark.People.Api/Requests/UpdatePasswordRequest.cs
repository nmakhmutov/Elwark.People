namespace Elwark.People.Api.Requests
{
    public class UpdatePasswordRequest
    {
        public UpdatePasswordRequest(string current, string password)
        {
            Current = current;
            Password = password;
        }

        public string Current { get; }

        public string Password { get; }
    }
}