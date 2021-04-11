using System;
using System.Globalization;
using People.Domain.AggregateModels.Account.Identities;

namespace People.Api.Infrastructure.Provider.Social.Google
{
    public class GoogleAccount
    {
        public GoogleAccount(GoogleIdentity identity, EmailIdentity email, bool isEmailVerified, string? firstName,
            string? lastName, Uri? picture, CultureInfo? locale)
        {
            Identity = identity;
            Email = email;
            IsEmailVerified = isEmailVerified;
            FirstName = firstName;
            LastName = lastName;
            Picture = picture;
            Locale = locale;
        }

        public GoogleIdentity Identity { get; }

        public EmailIdentity Email { get; }

        public bool IsEmailVerified { get; }

        public string? FirstName { get; }

        public string? LastName { get; }

        public Uri? Picture { get; }

        public CultureInfo? Locale { get; }
    }
}
