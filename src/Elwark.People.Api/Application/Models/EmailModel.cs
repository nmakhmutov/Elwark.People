using Elwark.People.Abstractions;

namespace Elwark.People.Api.Application.Models
{
    public class EmailModel
    {
        public EmailModel(Notification email, bool isConfirmed)
        {
            Email = email;
            IsConfirmed = isConfirmed;
        }

        public Notification Email { get; }
        
        public bool IsConfirmed { get; }
    }
}