namespace Elwark.People.Api.Application.Models.Requests
{
    public class CreatePasswordRequest
    {
        public CreatePasswordRequest(long code, string password)
        {
            Code = code;
            Password = password;
        }

        public long Code { get; }
        
        public string Password { get; }
    }
}