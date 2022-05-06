using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using People.Domain.Aggregates.AccountAggregate.Connections;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Events;
using People.Domain.Exceptions;
using People.Domain.Seed;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
namespace People.Domain.Aggregates.AccountAggregate;

public sealed class Account : HistoricEntity<AccountId>, IAggregateRoot
{
    public static readonly Uri DefaultPicture =
        new("https://res.cloudinary.com/elwark/image/upload/v1610430646/People/default_j21xml.png");

    private List<Connection> _connections;
    private Password? _password;
    private Registration _registration;
    private HashSet<string> _roles;

    public Account(AccountId id, Name name, Language language, Uri picture, IPAddress ip)
    {
        Id = id;
        Ban = null;
        Name = name;
        Version = int.MinValue;
        TimeZone = TimeZone.Utc;
        DateFormat = DateFormat.Default;
        TimeFormat = TimeFormat.Default;
        WeekStart = DayOfWeek.Monday;
        CountryCode = CountryCode.Empty;
        Language = language;
        Picture = picture;
        LastSignIn = DateTime.UnixEpoch;
        _password = null;
        _roles = new HashSet<string>();
        _connections = new List<Connection>();
        _registration = new Registration(Array.Empty<byte>(), CountryCode.Empty);

        AddDomainEvent(new AccountCreatedDomainEvent(this, ip));
    }

    public Name Name { get; private set; }

    public Uri Picture { get; private set; }

    public Language Language { get; private set; }

    public CountryCode CountryCode { get; private set; }

    public TimeZone TimeZone { get; private set; }

    public TimeFormat TimeFormat { get; private set; }

    public DateFormat DateFormat { get; private set; }

    public DayOfWeek WeekStart { get; private set; }

    public Ban? Ban { get; private set; }

    public DateTime LastSignIn { get; private set; }

    public bool IsActivated { get; private set; }

    public IReadOnlyCollection<string> Roles =>
        _roles;

    public IReadOnlyCollection<Connection> Connections =>
        _connections.AsReadOnly();

    public int Version { get; set; }

    public EmailConnection AddIdentity(EmailIdentity email, bool isConfirmed, DateTime now)
    {
        var isPrimaryAvailable = _connections
            .OfType<EmailConnection>()
            .Any(x => x.IsPrimary);

        var connection = new EmailConnection(email, now);

        if (isConfirmed)
            connection.Confirm(now);

        if (!isPrimaryAvailable)
            connection.SetPrimary();

        _connections.Add(connection);
        IsActivated = _connections.Any(x => x.IsConfirmed);

        return connection;
    }

    public GoogleConnection AddIdentity(GoogleIdentity google, string? firstName, string? lastName, DateTime now)
    {
        var connection = new GoogleConnection(google, firstName, lastName, now);
        connection.Confirm(now);

        _connections.Add(connection);
        IsActivated = _connections.Any(x => x.IsConfirmed);

        return connection;
    }

    public MicrosoftConnection AddIdentity(MicrosoftIdentity microsoft, string? firstName, string? lastName, DateTime now)
    {
        var connection = new MicrosoftConnection(microsoft, firstName, lastName, now);
        connection.Confirm(now);

        _connections.Add(connection);
        IsActivated = _connections.Any(x => x.IsConfirmed);

        return connection;
    }

    private bool IsConfirmed(Identity identity) =>
        GetIdentity(identity)?.IsConfirmed ?? false;

    public Connection? GetIdentity(Identity identity) =>
        _connections.FirstOrDefault(x => Equals(x.Identity, identity));

    public void ConfirmConnection(Identity identity, DateTime confirmedAt)
    {
        var connection = GetIdentity(identity) ?? throw new PeopleException(ExceptionCodes.IdentityNotFound);
        if (connection.IsConfirmed)
            return;

        connection.Confirm(confirmedAt);
        IsActivated = _connections.Any(x => x.IsConfirmed);
    }

    public void ConfuteConnection(Identity identity)
    {
        var connection = GetIdentity(identity) ?? throw new PeopleException(ExceptionCodes.IdentityNotFound);
        if (!connection.IsConfirmed)
            return;

        connection.Confute();
        IsActivated = _connections.Any(x => x.IsConfirmed);
    }

    public void DeleteIdentity(Identity identity)
    {
        var connection = GetIdentity(identity);
        switch (connection)
        {
            case null:
                return;

            case EmailConnection { IsPrimary: true }:
                throw new PeopleException(ExceptionCodes.PrimaryEmailCannotBeRemoved);

            default:
                _connections.Remove(connection);
                break;
        }
    }

    public void Update(Name name, CountryCode country, TimeZone timeZone, DateFormat dateFormat, TimeFormat timeFormat,
        DayOfWeek weekStart, Language language, Uri picture)
    {
        Name = name;
        CountryCode = country;
        TimeZone = timeZone;
        DateFormat = dateFormat;
        TimeFormat = timeFormat;
        WeekStart = weekStart;
        Language = language;
        Picture = picture;

        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }
    
    public void Update(Name name, CountryCode countryCode, TimeZone timeZone, Uri picture)
    {
        Name = name;
        CountryCode = countryCode;
        TimeZone = timeZone;
        Picture = picture;
        
        AddDomainEvent(new AccountUnbannedDomainEvent(this));
    }
    
    public void Update(Name name, Uri picture)
    {
        Name = name;
        Picture = picture;

        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public EmailConnection GetPrimaryEmail() =>
        _connections.OfType<EmailConnection>().First(x => x.IsPrimary);

    public bool IsActive()
    {
        if (!IsActivated)
            return false;

        if (Ban is not null)
            return false;

        return _password is null || _password.CreatedAt < LastSignIn;
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
            throw new PeopleException(ExceptionCodes.Internal, "Password not created");

        var hash = hasher.CreateHash(password, _password.Salt);
        return hash.SequenceEqual(_password.Hash);
    }

    public void SignIn(Identity identity, DateTime dateTime, IPAddress ip)
    {
        if (Ban is not null)
            throw new AccountBannedException(Ban);

        if (!IsConfirmed(identity))
            throw new PeopleException(ExceptionCodes.IdentityNotConfirmed);

        SignInSuccess(dateTime, ip);
    }

    public void SignIn(EmailIdentity email, DateTime dateTime, IPAddress ip, string password, IPasswordHasher hasher)
    {
        if (Ban is not null)
            throw new AccountBannedException(Ban);

        if (!IsConfirmed(email))
            throw new PeopleException(ExceptionCodes.IdentityNotConfirmed);

        if (!IsPasswordAvailable())
            throw new PeopleException(ExceptionCodes.PasswordNotCreated);

        if (!IsPasswordEqual(password, hasher))
            throw new PeopleException(ExceptionCodes.PasswordMismatch);

        SignInSuccess(dateTime, ip);
    }

    private void SignInSuccess(DateTime dateTime, IPAddress ip)
    {
        if (LastSignIn > dateTime)
            return;

        LastSignIn = dateTime;

        AddDomainEvent(new AccountSignInSuccess(this, ip));
    }

    public void AddRole(string role) =>
        _roles.Add(role);

    public void DeleteRole(string role) =>
        _roles.Remove(role);

    public void SetRegistration(IPAddress ip, CountryCode code, IIpAddressHasher ipHasher)
    {
        if (!_registration.IsEmpty)
            return;

        _registration = new Registration(ipHasher.CreateHash(ip), code);
    }

    public EmailConnection SetAsPrimaryEmail(EmailIdentity email)
    {
        var emails = _connections
            .OfType<EmailConnection>()
            .ToArray();

        var result = emails.FirstOrDefault(x => x.Value == email.Value)
                     ?? throw new PeopleException(ExceptionCodes.IdentityNotFound);

        if (!result.IsConfirmed)
            throw new PeopleException(ExceptionCodes.IdentityNotConfirmed);

        foreach (var item in emails)
            _ = item.Value == email.Value
                ? item.SetPrimary()
                : item.RemovePrimary();

        return result;
    }

    public void SetBan(string reason, DateTime expiredAt)
    {
        Ban = new Ban(reason, DateTime.UtcNow, expiredAt);
        AddDomainEvent(new AccountBannedDomainEvent(this));
    }

    public void Unban()
    {
        Ban = null;

        AddDomainEvent(new AccountUnbannedDomainEvent(this));
    }
}
