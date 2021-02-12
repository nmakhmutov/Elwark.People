using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.Events;
using People.Domain.Exceptions;
using People.Domain.SeedWork;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
namespace People.Domain.AggregateModels.Account
{
    public sealed class Account : Entity<AccountId>, IAggregateRoot
    {
        private HashSet<string> _roles;
        private List<IdentityModel> _identities;
        private Password? _password;
        private Registration _registration;
        private DateTime _lastSignIn;

        public Account(Name name, Language language, Uri picture, IPAddress ip)
        {
            var now = DateTime.UtcNow;
            Id = long.MinValue;
            Version = int.MinValue;
            _password = null;
            Ban = null;
            _roles = new HashSet<string>();
            _identities = new List<IdentityModel>();
            UpdatedAt = _lastSignIn = now;
            Name = name;
            Timezone = Timezone.Default;
            Address = new Address(CountryCode.Empty, string.Empty);
            Profile = new Profile(language, Gender.Female, picture);
            _registration = new Registration(ip.ToString(), CountryCode.Empty, now);

            AddDomainEvent(new AccountCreatedDomainEvent(this));
        }

        public int Version { get; set; }

        public Name Name { get; private set; }

        public Address Address { get; private set; }

        public Timezone Timezone { get; private set; }

        public Profile Profile { get; private set; }

        public Ban? Ban { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        public IReadOnlyCollection<string> Roles => _roles;

        public IReadOnlyCollection<IdentityModel> Identities => _identities.AsReadOnly();

        public void AddEmail(MailAddress email, EmailType type, bool isConfirmed)
        {
            var now = DateTime.UtcNow;
            _identities.Add(new EmailIdentityModel(email, type, isConfirmed ? now : null));
            UpdatedAt = now;
        }

        public void AddGoogle(GoogleIdentity identity, string name)
        {
            var now = DateTime.UtcNow;
            _identities.Add(new GoogleIdentityModel(identity.Value, name, now));
            UpdatedAt = now;
        }

        public void AddMicrosoft(MicrosoftIdentity identity, string name)
        {
            var now = DateTime.UtcNow;
            _identities.Add(new MicrosoftIdentityModel(identity.Value, name, now));
            UpdatedAt = now;
        }

        public bool IsConfirmed() =>
            _identities.Any(x => x.ConfirmedAt.HasValue);

        public bool IsConfirmed(Identity key) =>
            _identities.Any(x => x.Type == key.Type && x.Value == key.Value && x.IsConfirmed());

        public void ConfirmIdentity(Identity key, DateTime confirmedAt)
        {
            var identity = _identities.First(x => x.GetIdentity() == key);
            identity.SetAsConfirmed(confirmedAt);
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetName(Name name)
        {
            Name = name;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetProfile(Profile profile)
        {
            Profile = profile;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetAddress(Address address)
        {
            Address = address;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetTimezone(Timezone timezone)
        {
            Timezone = timezone;
            UpdatedAt = DateTime.UtcNow;
        }

        public AccountEmail GetPrimaryEmail()
        {
            var identity = _identities
                .Where(x => x.Type == IdentityType.Email)
                .Cast<EmailIdentityModel>()
                .First(x => x.EmailType == EmailType.Primary);

            return new AccountEmail(identity.EmailType, identity.Value, identity.IsConfirmed());
        }

        public bool IsActive()
        {
            if (!IsConfirmed())
                return false;

            if (Ban is not null)
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
                throw new ElwarkException(ElwarkExceptionCodes.Internal, "Password not created");

            var hash = hasher(password, _password.Salt);
            return hash.SequenceEqual(_password.Hash);
        }

        public void SignInSuccess(DateTime dateTime, IPAddress ip)
        {
            if (_lastSignIn > dateTime)
                return;

            _lastSignIn = dateTime;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new AccountSignInSuccess(this, ip));
        }

        public void SetNewId(AccountId id)
        {
            if (Id != long.MinValue)
                throw new ElwarkException(ElwarkExceptionCodes.Internal, "Account Id already created");

            Id = id;
        }

        public void AddRole(string role) => _roles.Add(role);

        public void UpdateRegistrationCountry(CountryCode code)
        {
            if (_registration.CountryCode != CountryCode.Empty)
                return;

            _registration = _registration with {CountryCode = code};
        }

        public string GetRegisteredIp() => _registration.Ip;
    }
}