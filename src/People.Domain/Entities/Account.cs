using System.Net;
using System.Net.Mail;
using People.Domain.DomainEvents;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using TimeZone = People.Domain.ValueObjects.TimeZone;

// ReSharper disable NotAccessedField.Local

namespace People.Domain.Entities;

public sealed class Account : Entity<long>,
    IAggregateRoot
{
    private const string DefaultPicture =
        "https://res.cloudinary.com/elwark/image/upload/v1660058875/People/default.svg";

    private readonly List<EmailAccount> _emails;
    private readonly List<ExternalConnection> _externals;

    private Ban? _ban;
    private DateTime _createdAt;
    private DateTime _lastActive;
    private DateTime _lastLogIn;
    private CountryCode _regCountryCode;
    private byte[] _regIp;
    private string[] _roles;
    private DateTime _updatedAt;

    private Account()
    {
        Name = new Name("Empty");
        Picture = DefaultPicture;
        _emails = new List<EmailAccount>();
        _roles = Array.Empty<string>();
        _externals = new List<ExternalConnection>();
        _regCountryCode = CountryCode.Empty;
        _regIp = Array.Empty<byte>();
    }

    public Account(string nickname, Language language, IPAddress ip, IIpHasher hasher)
        : this()
    {
        Name = new Name(nickname);
        Picture = DefaultPicture;
        Language = language;
        CountryCode = CountryCode.Empty;
        TimeZone = TimeZone.Utc;
        TimeFormat = TimeFormat.Default;
        DateFormat = DateFormat.Default;
        StartOfWeek = DayOfWeek.Monday;
        IsActivated = false;
        _createdAt = _updatedAt = _lastActive = _lastLogIn = DateTime.MinValue;
        _ban = null;
        _emails = new List<EmailAccount>();
        _roles = Array.Empty<string>();
        _externals = new List<ExternalConnection>();
        _regCountryCode = CountryCode.Empty;
        _regIp = hasher.CreateHash(ip);

        AddDomainEvent(new AccountCreatedDomainEvent(this, ip));
    }

    public Name Name { get; private set; }

    public string Picture { get; private set; }

    public Language Language { get; private set; }

    public CountryCode CountryCode { get; private set; }

    public TimeZone TimeZone { get; private set; }

    public DateFormat DateFormat { get; private set; }

    public TimeFormat TimeFormat { get; private set; }

    public DayOfWeek StartOfWeek { get; private set; }

    public bool IsActivated { get; private set; }

    public bool IsBaned =>
        _ban is not null;

    public IReadOnlyCollection<string> Roles =>
        _roles;

    public IReadOnlyCollection<EmailAccount> Emails =>
        _emails.AsReadOnly();

    public IReadOnlyCollection<ExternalConnection> Externals =>
        _externals.AsReadOnly();

    public void SetAsUpdated(TimeProvider provider)
    {
        if (_createdAt == DateTime.MinValue)
            _createdAt = provider.UtcNow();

        _updatedAt = provider.UtcNow();
    }

    public DateTime GetCreatedDateTime() =>
        _createdAt;

    public void AddEmail(MailAddress email, bool isConfirmed, TimeProvider timeProvider)
    {
        var notConfirmed = _emails.FirstOrDefault(x => !x.IsConfirmed);
        if (notConfirmed is not null)
            throw EmailException.NotConfirmed(new MailAddress(notConfirmed.Email));

        var now = timeProvider.UtcNow();
        _emails.Add(new EmailAccount(Id, email.Address, _emails.Count == 0, isConfirmed ? now : null, now));

        UpdateActivation();
    }

    public MailAddress GetPrimaryEmail() =>
        new(_emails.First(x => x.IsPrimary).Email);

    public void SetPrimaryEmail(MailAddress email)
    {
        var result = _emails.FirstOrDefault(x => x.Email == email.Address) ?? throw EmailException.NotFound(email);

        if (!result.IsConfirmed)
            throw EmailException.NotConfirmed(email);

        foreach (var item in _emails)
            item.RemovePrimary();

        result.SetPrimary();
    }

    public void ConfirmEmail(MailAddress email, TimeProvider timeProvider)
    {
        var result = _emails.FirstOrDefault(x => x.Email == email.Address) ?? throw EmailException.NotFound(email);
        result.Confirm(timeProvider.UtcNow());

        UpdateActivation();
        AddDomainEvent(new EmailConfirmedDomainEvent(this, email));
    }

    public void DeleteEmail(MailAddress email)
    {
        var result = _emails.FirstOrDefault(x => x.Email == email.Address);
        if (result is null)
            return;

        if (result.IsPrimary)
            throw AccountException.PrimaryEmailCannotBeRemoved(Id);

        _emails.Remove(result);

        UpdateActivation();
    }

    public void AddGoogle(string identity, string? firstName, string? lastName, TimeProvider timeProvider)
    {
        _externals.Add(ExternalConnection.Google(identity, firstName, lastName, timeProvider.UtcNow()));
        Name = new Name(Name.Nickname, Name.FirstName ?? firstName, Name.LastName ?? lastName, Name.PreferNickname);

        UpdateActivation();
        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void DeleteGoogle(string identity)
    {
        _externals.RemoveAll(x => x.Type == ExternalService.Google && x.Identity == identity);

        UpdateActivation();
    }

    public void AddMicrosoft(string identity, string? firstName, string? lastName, TimeProvider timeProvider)
    {
        _externals.Add(ExternalConnection.Microsoft(identity, firstName, lastName, timeProvider.UtcNow()));
        Name = new Name(Name.Nickname, Name.FirstName ?? firstName, Name.LastName ?? lastName, Name.PreferNickname);

        UpdateActivation();
        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void DeleteMicrosoft(string identity)
    {
        _externals.RemoveAll(x => x.Type == ExternalService.Microsoft && x.Identity == identity);

        UpdateActivation();
    }

    public void AddRole(string role) =>
        _roles = _roles.Append(role).Distinct().ToArray();

    public void DeleteRole(string role) =>
        _roles = _roles.Where(x => x != role).ToArray();

    public void Ban(string reason, DateTime expiredAt, TimeProvider timeProvider)
    {
        _ban = new Ban(reason, expiredAt, timeProvider.UtcNow());

        AddDomainEvent(new AccountBannedDomainEvent(this, reason, expiredAt));
    }

    public void Unban()
    {
        _ban = null;

        AddDomainEvent(new AccountUnbannedDomainEvent(this));
    }

    public void Update(string? firstName, string? lastName) =>
        Update(Name.Nickname, firstName, lastName, Name.PreferNickname);

    public void Update(string nickname, string? firstName, string? lastName, bool preferNickname)
    {
        Name = new Name(nickname, firstName, lastName, preferNickname);

        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void Update(Uri? picture)
    {
        Picture = picture?.ToString() ?? DefaultPicture;

        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void Update(Language language, CountryCode country, TimeZone timeZone)
    {
        Language = language;
        CountryCode = country;
        TimeZone = timeZone;

        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void Update(DateFormat dateFormat, TimeFormat timeFormat, DayOfWeek weekStart)
    {
        DateFormat = dateFormat;
        TimeFormat = timeFormat;
        StartOfWeek = weekStart;

        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void UpdateRegistrationCountry(CountryCode code)
    {
        CountryCode = code;

        if (_regCountryCode != CountryCode.Empty)
            return;

        _regCountryCode = code;
    }

    private void UpdateActivation() =>
        IsActivated = _externals.Count > 0 || _emails.Any(x => x is { IsPrimary: true, IsConfirmed: true });
}
