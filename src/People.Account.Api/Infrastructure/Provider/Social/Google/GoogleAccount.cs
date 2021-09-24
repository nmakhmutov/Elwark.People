using System;
using System.Globalization;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;

namespace People.Account.Api.Infrastructure.Provider.Social.Google
{
    public class GoogleAccount
    {
        public GoogleAccount(Identity.Google identity, Identity.Email email, bool isEmailVerified, string? firstName,
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

        public Identity.Google Identity { get; }

        public Identity.Email Email { get; }

        public bool IsEmailVerified { get; }

        public string? FirstName { get; }

        public string? LastName { get; }

        public Uri? Picture { get; }

        public CultureInfo? Locale { get; }
    }
}
