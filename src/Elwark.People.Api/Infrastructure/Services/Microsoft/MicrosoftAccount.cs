using Elwark.People.Abstractions;

namespace Elwark.People.Api.Infrastructure.Services.Microsoft
{
    public class MicrosoftAccount
    {
        public MicrosoftAccount(Identification.Microsoft id, Identification.Email email, string? firstName,
            string? lastName)
        {
            Id = id;
            Email = email;
            FirstName = firstName;
            LastName = lastName;
        }

        public Identification.Microsoft Id { get; }

        public Identification.Email Email { get; }

        public string? FirstName { get; }

        public string? LastName { get; }
    }
}