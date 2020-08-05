using System;
using Elwark.People.Abstractions;

namespace Elwark.People.Api.Infrastructure.Services.Facebook
{
    public class FacebookAccount
    {
        public FacebookAccount(Identification.Facebook id, Identification.Email email, Gender? gender,
            DateTime? birthday, string? firstName, string? lastName, Uri link, Uri? picture)
        {
            Id = id;
            Email = email;
            Gender = gender;
            Birthday = birthday;
            FirstName = firstName;
            LastName = lastName;
            Link = link;
            Picture = picture;
        }

        public Identification.Facebook Id { get; }

        public Identification.Email Email { get; }

        public Gender? Gender { get; }

        public DateTime? Birthday { get; }

        public string? FirstName { get; }

        public string? LastName { get; }

        public Uri Link { get; }

        public Uri? Picture { get; }
    }
}