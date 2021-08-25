using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Events;
using People.Domain.Exceptions;
using People.Domain.SeedWork;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
namespace People.Domain.Aggregates.AccountAggregate
{
    public sealed class Account : Entity<AccountId>, IAggregateRoot
    {
        public static Uri DefaultPicture =>
            new("https://res.cloudinary.com/elwark/image/upload/v1610430646/People/default_j21xml.png");

        private List<Connection> _connections;
        private DateTime _lastSignIn;
        private Password? _password;
        private Registration _registration;
        private HashSet<string> _roles;

        public Account(AccountId id, Name name, Language language, Uri picture, IPAddress ip)
        {
            var now = DateTime.UtcNow;
            Id = id;
            Version = int.MinValue;
            Ban = null;
            Name = name;
            TimeInfo = TimeInfo.Default;
            Address = new Address(CountryCode.Empty, string.Empty);
            Language = language;
            Gender = Gender.Female;
            Picture = picture;
            UpdatedAt = _lastSignIn = now;
            _password = null;
            _roles = new HashSet<string>();
            _connections = new List<Connection>();
            _registration = new Registration(Array.Empty<byte>(), CountryCode.Empty, now);

            AddDomainEvent(new AccountCreatedDomainEvent(this, ip));
        }

        public int Version { get; set; }

        public Name Name { get; private set; }

        public Address Address { get; private set; }

        public TimeInfo TimeInfo { get; private set; }

        public Language Language { get; private set; }

        public Gender Gender { get; private set; }

        public Uri Picture { get; private set; }

        public string? Bio { get; private set; }

        public DateTime? DateOfBirth { get; private set; }

        public Ban? Ban { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        public DateTime CreatedAt => _registration.CreatedAt;

        public IReadOnlyCollection<string> Roles => _roles;

        public IReadOnlyCollection<Connection> Connections => _connections.AsReadOnly();

        public void AddEmail(Identity.Email email, bool isConfirmed)
        {
            var now = DateTime.UtcNow;
            var emails = _connections
                .OfType<EmailConnection>()
                .ToArray();

            EmailType type;
            if (emails.Any(x => x.EmailType == EmailType.Primary) == false)
                type = EmailType.Primary;
            else if (emails.Any(x => x.EmailType == EmailType.Secondary) == false)
                type = EmailType.Secondary;
            else
                type = EmailType.None;

            _connections.Add(new EmailConnection(email.Value, type, isConfirmed ? now : null));
            UpdatedAt = now;
        }

        public void AddGoogle(Identity.Google identity, string? firstName, string? lastName)
        {
            var now = DateTime.UtcNow;
            _connections.Add(new GoogleConnection(identity.Value, firstName, lastName, now));
            UpdatedAt = now;
        }

        public void AddMicrosoft(Identity.Microsoft identity, string? firstName, string? lastName)
        {
            var now = DateTime.UtcNow;
            _connections.Add(new MicrosoftConnection(identity.Value, firstName, lastName, now));
            UpdatedAt = now;
        }

        public bool IsConfirmed() =>
            _connections.Any(x => x.ConfirmedAt.HasValue);

        private bool IsConfirmed(Identity key) =>
            _connections.Any(x => x.ConnectionType == key.Type && x.Value == key.Value && x.IsConfirmed);

        public Connection? GetIdentity(Identity key) =>
            _connections.FirstOrDefault(x => x.Identity == key);

        public void ConfirmConnection(Identity key, DateTime confirmedAt)
        {
            var identity = _connections.First(x => x.Identity == key);
            if (identity.IsConfirmed)
                return;

            identity.SetAsConfirmed(confirmedAt);

            UpdatedAt = DateTime.UtcNow;
        }

        public void DeleteIdentity(Identity key)
        {
            var identity = _connections.FirstOrDefault(x => x.Identity == key);
            switch (identity)
            {
                case null:
                    return;

                case EmailConnection {EmailType: EmailType.Primary}:
                    throw new ElwarkException(ElwarkExceptionCodes.PrimaryEmailCannotBeRemoved);

                default:
                    _connections.Remove(identity);
                    break;
            }
        }

        public void Update(Name name, Address address, TimeInfo timeInfo, Language language, Gender gender, Uri picture,
            string? bio = null, DateTime? dateOfBirth = null)
        {
            Name = name;
            Address = address;
            TimeInfo = timeInfo;
            Language = language;
            Gender = gender;
            Picture = picture;
            Bio = bio;
            DateOfBirth = dateOfBirth;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new AccountUpdatedDomainEvent(Id, UpdatedAt));
        }

        public void Update(Name name, Uri picture)
        {
            Name = name;
            Picture = picture;
            
            AddDomainEvent(new AccountUpdatedDomainEvent(Id, UpdatedAt));
        }

        public EmailConnection GetPrimaryEmail() =>
            _connections.OfType<EmailConnection>().First(x => x.EmailType == EmailType.Primary);

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

        public void SetPassword(string password, IPasswordHasher hasher)
        {
            if (password.Length > Password.MaxLength)
                throw new ArgumentOutOfRangeException(nameof(password), "User password too long");
            
            var salt = hasher.CreateSalt();
            var hash = hasher.CreateHash(password, salt);
            _password = new Password(hash, salt, DateTime.UtcNow);
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsPasswordEqual(string password, IPasswordHasher hasher)
        {
            if (_password is null)
                throw new ElwarkException(ElwarkExceptionCodes.Internal, "Password not created");

            var hash = hasher.CreateHash(password, _password.Salt);
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

        public void SignIn(Identity.Email identity, DateTime dateTime, IPAddress ip, string password,
            IPasswordHasher hasher)
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

        public void AddRole(string role) =>
            _roles.Add(role);

        public void SetRegistration(IPAddress ip, CountryCode code, IIpAddressHasher ipHasher)
        {
            if (!_registration.IsEmpty)
                return;

            _registration = _registration with
            {
                Ip = ipHasher.CreateHash(ip),
                CountryCode = code
            };
        }

        public void ChangeEmailType(Identity.Email email, EmailType type)
        {
            var emails = _connections
                .OfType<EmailConnection>()
                .ToArray();

            var result = emails.FirstOrDefault(x => x.Value == email.Value);
            if (result is null)
                throw new ElwarkException(ElwarkExceptionCodes.IdentityNotFound);
            
            if (!result.IsConfirmed)
                throw new ElwarkException(ElwarkExceptionCodes.IdentityNotConfirmed);
            
            if (result.EmailType == EmailType.Primary)
                throw new ElwarkException(ElwarkExceptionCodes.PrimaryEmailCannotBeRemoved);
            
            if (type == EmailType.Primary)
                emails.First(x => x.EmailType == EmailType.Primary)
                    .ChangeType(EmailType.Secondary);

            result.ChangeType(type);
        }
    }
}
