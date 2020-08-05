using System;
using System.Globalization;
using Elwark.People.Abstractions;

namespace Elwark.People.Api.Infrastructure.Services.Google
{
    public class GoogleAccount
    {
        public GoogleAccount(Identification.Google id, Identification.Email email, bool isEmailVerified,
            string? firstName, string? lastName, Uri? picture, CultureInfo? locale)
        {
            Id = id;
            Email = email;
            IsEmailVerified = isEmailVerified;
            FirstName = firstName;
            LastName = lastName;
            Picture = picture;
            Locale = locale;
        }

        public Identification.Google Id { get; }

        public Identification.Email Email { get; }

        public bool IsEmailVerified { get; }

        public string? FirstName { get; }

        public string? LastName { get; }

        public Uri? Picture { get; }

        public CultureInfo? Locale { get; }
    }
}