using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Account.Domain.Events;
using People.Account.Domain.Exceptions;
using People.Account.Domain.Seed;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
namespace People.Account.Domain.Aggregates.AccountAggregate
{
    public sealed class Account : HistoricEntity<AccountId>, IAggregateRoot
    {
        public static readonly Uri DefaultPicture =
            new("https://res.cloudinary.com/elwark/image/upload/v1610430646/People/default_j21xml.png");

        private List<Connection> _connections;
        private DateTime _lastSignIn;
        private Password? _password;
        private Registration _registration;
        private HashSet<string> _roles;

        public Account(AccountId id, Name name, Language language, Uri picture, IPAddress ip)
        {
            Id = id;
            Version = int.MinValue;
            Ban = null;
            Name = name;
            TimeZone = TimeZoneInfo.Utc.Id;
            FirstDayOfWeek = DayOfWeek.Monday;
            CountryCode = CountryCode.Empty;
            Language = language;
            Picture = picture;
            _lastSignIn = DateTime.MinValue;
            _password = null;
            _roles = new HashSet<string>();
            _connections = new List<Connection>();
            _registration = new Registration(Array.Empty<byte>(), CountryCode.Empty);

            AddDomainEvent(new AccountCreatedDomainEvent(this, ip));
        }

        public int Version { get; set; }

        public Name Name { get; private set; }

        public CountryCode CountryCode { get; private set; }

        public string TimeZone { get; private set; }

        public DayOfWeek FirstDayOfWeek { get; private set; }

        public Language Language { get; private set; }

        public Uri Picture { get; private set; }

        public Ban? Ban { get; private set; }

        public IReadOnlyCollection<string> Roles => _roles;

        public IReadOnlyCollection<Connection> Connections => _connections.AsReadOnly();

        public EmailConnection AddEmail(Identity.Email email, bool isConfirmed, DateTime now)
        {
            var isPrimaryAvailable = _connections
                .OfType<EmailConnection>()
                .Any(x => x.IsPrimary);

            var connection = new EmailConnection(email.Value, now, !isPrimaryAvailable, isConfirmed ? now : null);

            _connections.Add(connection);

            return connection;
        }

        public GoogleConnection AddGoogle(Identity.Google identity, string? firstName, string? lastName, DateTime now)
        {
            var connection = new GoogleConnection(identity.Value, firstName, lastName, now, now);
            _connections.Add(connection);

            return connection;
        }

        public MicrosoftConnection AddMicrosoft(Identity.Microsoft identity, string? firstName, string? lastName,
            DateTime now)
        {
            var connection = new MicrosoftConnection(identity.Value, firstName, lastName, now, now);
            _connections.Add(connection);

            return connection;
        }

        public bool IsConfirmed() =>
            _connections.Any(x => x.ConfirmedAt.HasValue);

        private bool IsConfirmed(Identity key) =>
            _connections.Any(x => x.Type == key.Type && x.Value == key.Value && x.IsConfirmed);

        public Connection? GetIdentity(Identity key) =>
            _connections.FirstOrDefault(x => x.Identity == key);

        public void ConfirmConnection(Identity key, DateTime confirmedAt)
        {
            var identity = _connections.First(x => x.Identity == key);
            if (identity.IsConfirmed)
                return;

            identity.SetAsConfirmed(confirmedAt);
        }

        public void DeleteIdentity(Identity key)
        {
            var identity = _connections.FirstOrDefault(x => x.Identity == key);
            switch (identity)
            {
                case null:
                    return;

                case EmailConnection { IsPrimary: true }:
                    throw new ElwarkException(ElwarkExceptionCodes.PrimaryEmailCannotBeRemoved);

                default:
                    _connections.Remove(identity);
                    break;
            }
        }

        public void Update(Name name, CountryCode countryCode, string timeZone, DayOfWeek firstDayOfWeek,
            Language language, Uri picture)
        {
            Name = name;
            CountryCode = countryCode;
            TimeZone = GetTimeZone(timeZone);
            FirstDayOfWeek = firstDayOfWeek;
            Language = language;
            Picture = picture;

            AddDomainEvent(new AccountUpdatedDomainEvent(Id, UpdatedAt));
        }

        public void Update(Name name, Uri picture)
        {
            Name = name;
            Picture = picture;

            AddDomainEvent(new AccountUpdatedDomainEvent(Id, UpdatedAt));
        }

        public EmailConnection GetPrimaryEmail() =>
            _connections
                .OfType<EmailConnection>()
                .First(x => x.IsPrimary);

        public bool IsActive()
        {
            if (!IsConfirmed())
                return false;

            if (Ban is not null)
                return false;

            return _password is null || _password.CreatedAt <= _lastSignIn;
        }

        public bool IsPasswordAvailable() =>
            _password is not null;

        public void SetPassword(string password, IPasswordHasher hasher, DateTime now)
        {
            if (password.Length > Password.MaxLength)
                throw new ArgumentOutOfRangeException(nameof(password), "User password too long");

            var salt = hasher.CreateSalt();
            var hash = hasher.CreateHash(password, salt);
            _password = new Password(hash, salt, now);
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

        public void SignIn(Identity.Email email, DateTime dateTime, IPAddress ip, string password,
            IPasswordHasher hasher)
        {
            if (Ban is not null)
                throw new AccountBannedException(Ban);

            if (!IsConfirmed(email))
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

        public EmailConnection SetAsPrimaryEmail(Identity.Email email)
        {
            var emails = _connections
                .OfType<EmailConnection>()
                .ToList();

            var result = emails.FirstOrDefault(x => x.Value == email.Value);
            if (result is null)
                throw new ElwarkException(ElwarkExceptionCodes.IdentityNotFound);

            if (!result.IsConfirmed)
                throw new ElwarkException(ElwarkExceptionCodes.IdentityNotConfirmed);

            foreach (var item in emails)
                _ = item.Value == email.Value
                    ? item.SetPrimary()
                    : item.RemovePrimary();

            return result;
        }

        private static string GetTimeZone(string timeZone)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZone).Id;
            }
            catch
            {
                return TimeZoneInfo.Utc.Id;
            }
        }
    }
}
