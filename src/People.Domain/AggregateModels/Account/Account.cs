using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.Events;
using People.Domain.SeedWork;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.AggregateModels.Account
{
    public sealed class Account : Entity<AccountId>, IAggregateRoot
    {
        private HashSet<string> _roles;
        private List<Identity> _identities;
        private Password? _password;

        public Account(AccountId id, Name name, Language language, Uri picture)
        {
            var now = DateTime.UtcNow;
            Id = id;
            Version = long.MinValue;
            _password = null;
            Ban = null;
            _roles = new HashSet<string>();
            _identities = new List<Identity>();
            UpdatedAt = LoggedInAt = now;
            Name = name;
            Timezone = Timezone.Default;
            Address = new Address(CountryCode.Empty, string.Empty);
            Profile = new Profile(language, Gender.Female, picture);
            Registration = new Registration(Array.Empty<byte>(), CountryCode.Empty, now);

            AddDomainEvent(new AccountCreatedDomainEvent(this));
        }

        public long Version { get; set; }

        public Name Name { get; private set; }

        public Address Address { get; private set; }

        public Timezone Timezone { get; private set; }

        public Profile Profile { get; private set; }

        public Ban? Ban { get; private set; }

        public Registration Registration { get; private set; }

        public DateTime LoggedInAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        public IReadOnlyCollection<string> Roles => _roles;

        public IReadOnlyCollection<Identity> Identities => _identities.AsReadOnly();

        public IReadOnlyCollection<IdentityKey> IdentityKeys() => _identities
            .Select(x => new IdentityKey(x.Type, x.Value))
            .ToArray();

        public void AddIdentity(Identity identity)
        {
            _identities.Add(identity);
        }

        public void ConfirmIdentity(IdentityKey key, DateTime confirmedAt)
        {
            var identity = _identities.First(x => x.GetKey() == key);
            identity.SetAsConfirmed(confirmedAt);
        }

        public void SetPassword(byte[] hash, byte[] salt)
        {
            _password = new Password(hash, salt, DateTime.UtcNow);
        }

        public void SetProfile(Profile profile)
        {
            Profile = profile;
        }

        public MailAddress GetPrimaryEmail()
        {
            var identity = _identities
                .Where(x => x.Type == IdentityType.Email)
                .Cast<EmailIdentity>()
                .First(x => x.NotificationType == EmailNotificationType.Primary);

            return new MailAddress(identity.Value);
        }

        public bool IsConfirmed() =>
            _identities.Any(x => x.ConfirmedAt.HasValue);
    }
}