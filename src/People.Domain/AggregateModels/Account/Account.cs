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
        private List<IdentityModel> _identities;
        private DateTime _lastSignIn;
        private Password? _password;
        private Registration _registration;
        private HashSet<string> _roles;

        public Account(Name name, Language language, Uri picture, IPAddress ip)
        {
            var now = DateTime.UtcNow;
            Id = long.MinValue;
            Version = int.MinValue;
            Ban = null;
            Name = name;
            Timezone = Timezone.Default;
            Address = new Address(CountryCode.Empty, string.Empty);
            Profile = new Profile(language, Gender.Female, picture);
            UpdatedAt = _lastSignIn = now;
            _password = null;
            _roles = new HashSet<string>();
            _identities = new List<IdentityModel>();
            _registration = new Registration(Array.Empty<byte>(), CountryCode.Empty, now);

            AddDomainEvent(new AccountCreatedDomainEvent(this, ip));
        }

        public int Version { get; set; }

        public Name Name { get; private set; }

        public Address Address { get; private set; }

        public Timezone Timezone { get; private set; }

        public Profile Profile { get; private set; }

        public Ban? Ban { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        public DateTime CreatedAt => _registration.CreatedAt;

        public IReadOnlyCollection<string> Roles => _roles;

        public IReadOnlyCollection<IdentityModel> Identities => _identities.AsReadOnly();

        public void AddEmail(MailAddress email, bool isConfirmed)
        {
            var now = DateTime.UtcNow;
            var emails = _identities
                .Where(x => x.Type == IdentityType.Email)
                .Cast<EmailIdentityModel>()
                .ToArray();

            EmailType type;
            if (emails.Any(x => x.EmailType == EmailType.Primary) == false)
                type = EmailType.Primary;
            else if (emails.Any(x => x.EmailType == EmailType.Secondary) == false)
                type = EmailType.Secondary;
            else
                type = EmailType.None;

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

        private bool IsConfirmed(Identity key) =>
            _identities.Any(x => x.Type == key.Type && x.Value == key.Value && x.IsConfirmed());

        public IdentityModel? GetIdentity(Identity key) =>
            _identities.FirstOrDefault(x => x.GetIdentity() == key);

        public void ConfirmIdentity(Identity key, DateTime confirmedAt)
        {
            var identity = _identities.First(x => x.GetIdentity() == key);
            if(identity.IsConfirmed())
                return;
            
            identity.SetAsConfirmed(confirmedAt);
            
            UpdatedAt = DateTime.UtcNow;
        }

        public void DeleteIdentity(Identity key)
        {
            var identity = _identities.FirstOrDefault(x => x.GetIdentity() == key);
            switch (identity)
            {
                case null:
                    return;
                
                case EmailIdentityModel {EmailType: EmailType.Primary}:
                    throw new ElwarkException(ElwarkExceptionCodes.PrimaryEmailCannotBeRemoved);
                
                default:
                    _identities.Remove(identity);
                    break;
            }
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

        public void SignIn(Identity identity, DateTime dateTime, IPAddress ip)
        {
            if (Ban is not null)
                throw new AccountBannedException(Ban);

            if (!IsConfirmed(identity))
                throw new ElwarkException(ElwarkExceptionCodes.IdentityNotConfirmed);

            SignInSuccess(dateTime, ip);
        }

        public void SignIn(EmailIdentity identity, DateTime dateTime, IPAddress ip, string password,
            Func<string, byte[], byte[]> hasher)
        {
            if (Ban is not null)
                throw new AccountBannedException(Ban);

            if (!IsConfirmed(identity))
                throw new ElwarkException(ElwarkExceptionCodes.IdentityNotConfirmed);

            if (!IsPasswordAvailable())
                throw new ElwarkException(ElwarkExceptionCodes.PasswordNotCreated);

            if (!IsPasswordEqual(password, hasher))
                throw new ElwarkException(ElwarkExceptionCodes.PasswordMismatch);

            SignInSuccess(dateTime, ip);
        }

        private void SignInSuccess(DateTime dateTime, IPAddress ip)
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

        public void AddRole(string role) => 
            _roles.Add(role);

        public void SetRegistration(IPAddress ip, CountryCode code, Func<IPAddress, byte[]> ipHasher)
        {
            if (!_registration.IsEmpty())
                return;

            _registration = _registration with
            {
                Ip = ipHasher(ip),
                CountryCode = code
            };
        }

        public void ChangeEmailType(EmailIdentity email, EmailType type)
        {
            var emails = _identities.Where(x => x.Type == IdentityType.Email)
                .Cast<EmailIdentityModel>()
                .ToArray();
            
            var result = emails.FirstOrDefault(x => x.Value == email.Value);
            if (result is null)
                throw new ElwarkException(ElwarkExceptionCodes.IdentityNotFound);

            if (result.EmailType == EmailType.Primary)
                throw new ElwarkException(ElwarkExceptionCodes.PrimaryEmailCannotBeRemoved);
            
            if (!result.IsConfirmed())
                throw new ElwarkException(ElwarkExceptionCodes.IdentityNotConfirmed);

            if (type == EmailType.Primary)
                emails.First(x => x.EmailType == EmailType.Primary)
                    .ChangeType(EmailType.Secondary);

            result.ChangeType(type);
        }
    }
}