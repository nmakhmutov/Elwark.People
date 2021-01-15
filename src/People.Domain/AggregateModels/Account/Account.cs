using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private Registration _registration;
        private DateTime _lastSignIn;

        public Account(AccountId id, Name name, Language language, Uri picture, IPAddress ip)
        {
            var now = DateTime.UtcNow;
            Id = id;
            Version = long.MinValue;
            _password = null;
            Ban = null;
            _roles = new HashSet<string>();
            _identities = new List<Identity>();
            UpdatedAt = _lastSignIn = now;
            Name = name;
            Timezone = Timezone.Default;
            Address = new Address(CountryCode.Empty, string.Empty);
            Profile = new Profile(language, Gender.Female, picture);
            _registration = new Registration(ip.ToString(), CountryCode.Empty, now);

            AddDomainEvent(new AccountCreatedDomainEvent(this));
        }

        public long Version { get; set; }

        public Name Name { get; private set; }

        public Address Address { get; private set; }

        public Timezone Timezone { get; private set; }

        public Profile Profile { get; private set; }

        public Ban? Ban { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        public IReadOnlyCollection<string> Roles => _roles;

        public IReadOnlyCollection<Identity> Identities => _identities.AsReadOnly();

        public IReadOnlyCollection<IdentityKey> IdentityKeys() => _identities
            .Select(x => new IdentityKey(x.Type, x.Value))
            .ToArray();

        public bool IsBanned() =>
            Ban != null;
        
        public void AddIdentity(Identity identity)
        {
            _identities.Add(identity);
            UpdatedAt = DateTime.UtcNow;
        }

        public void ConfirmIdentity(IdentityKey key, DateTime confirmedAt)
        {
            var identity = _identities.First(x => x.GetKey() == key);
            identity.SetAsConfirmed(confirmedAt);
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetProfile(Profile profile)
        {
            Profile = profile;
            UpdatedAt = DateTime.UtcNow;
        }

        public AccountEmail GetPrimaryEmail()
        {
            var identity = _identities
                .Where(x => x.Type == IdentityType.Email)
                .Cast<EmailIdentity>()
                .First(x => x.EmailType == EmailType.Primary);

            return new AccountEmail(identity.EmailType, identity.Value, identity.IsConfirmed());
        }

        public bool IsConfirmed() =>
            _identities.Any(x => x.ConfirmedAt.HasValue);

        public bool IsActive()
        {
            if (!IsConfirmed())
                return false;

            if (IsBanned())
                return false;

            if (_password is not null && _password.CreatedAt > _lastSignIn) 
                return false;

            return true;
        }
        
        public bool IsPasswordAvailable() => 
            _password is not null;

        public void SetPassword(string password, byte[] salt, Func<string, byte[], byte[]> hasher)
        {
            var hash = hasher(password, salt);
            _password = new Password(hash, salt, DateTime.UtcNow);
            UpdatedAt = DateTime.UtcNow;
        }
        
        public bool IsPasswordEqual(string password, Func<string, byte[], byte[]> hasher)
        {
            if (_password is null)
                throw new ArgumentNullException(nameof(password), "Password not created");

            var hash = hasher(password, _password.Salt);
            return hash.SequenceEqual(_password.Hash);
        }

        public void SignInSuccess(DateTime dateTime, IPAddress ip)
        {
            if(_lastSignIn > dateTime)
                return;
            
            _lastSignIn = dateTime;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new AccountSignInSuccess(this, ip));
        }
    }
}