using System.Net;
using System.Net.Mail;
using People.Domain.DomainEvents;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using TimeZone = People.Domain.ValueObjects.TimeZone;

// ReSharper disable NotAccessedField.Local

namespace People.Domain.Entities;

public sealed class Account : Entity<AccountId>,
    IAggregateRoot
{
    private const string DefaultPicture =
        "https://res.cloudinary.com/elwark/image/upload/v1660058875/People/default.svg";

    private readonly HashSet<EmailAccount> _emails;
    private readonly HashSet<ExternalConnection> _externals;

    private Ban? _ban;
    private DateTime _createdAt;
    private DateTime _lastActive;
    private DateTime _lastLogIn;
    private CountryCode _regCountryCode;
    private byte[] _regIp;
    private string[] _roles;
    private DateTime _updatedAt;

#pragma warning disable CS8618
    private Account()
    {
    }
#pragma warning restore CS8618

    public Account(string nickname, Language language, IPAddress ip, IIpHasher hasher)
        : this()
    {
        Name = new Name(nickname);
        Picture = DefaultPicture;
        RegionCode = RegionCode.Empty;
        CountryCode = CountryCode.Empty;
        Language = language;
        TimeZone = TimeZone.Utc;
        TimeFormat = TimeFormat.Default;
        DateFormat = DateFormat.Default;
        StartOfWeek = DayOfWeek.Monday;
        IsActivated = false;
        _createdAt = _updatedAt = _lastActive = _lastLogIn = DateTime.MinValue;
        _ban = null;
        _roles = Array.Empty<string>();
        _emails = new HashSet<EmailAccount>();
        _externals = new HashSet<ExternalConnection>();
        _regCountryCode = CountryCode.Empty;
        _regIp = hasher.CreateHash(ip);

        AddDomainEvent(new AccountCreatedDomainEvent(this, ip));
    }

    public Name Name { get; private set; }

    public string Picture { get; private set; }

    public RegionCode RegionCode { get; private set; }

    public CountryCode CountryCode { get; private set; }

    public Language Language { get; private set; }

    public TimeZone TimeZone { get; private set; }

    public DateFormat DateFormat { get; private set; }

    public TimeFormat TimeFormat { get; private set; }

    public DayOfWeek StartOfWeek { get; private set; }

    public bool IsActivated { get; private set; }

    public bool IsBaned =>
        _ban is not null;

    public IReadOnlyCollection<EmailAccount> Emails =>
        _emails.ToArray();

    public IReadOnlyCollection<ExternalConnection> Externals =>
        _externals.ToArray();

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
        AddDomainEvent(new EmailConfirmedDomainEvent(Id, email));
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
        AddDomainEvent(new AccountUpdatedDomainEvent(Id));
    }

    public void DeleteGoogle(string identity)
    {
        _externals.RemoveWhere(x => x.Type == ExternalService.Google && x.Identity == identity);

        UpdateActivation();
    }

    public void AddMicrosoft(string identity, string? firstName, string? lastName, TimeProvider timeProvider)
    {
        _externals.Add(ExternalConnection.Microsoft(identity, firstName, lastName, timeProvider.UtcNow()));
        Name = new Name(Name.Nickname, Name.FirstName ?? firstName, Name.LastName ?? lastName, Name.PreferNickname);

        UpdateActivation();
        AddDomainEvent(new AccountUpdatedDomainEvent(Id));
    }

    public void DeleteMicrosoft(string identity)
    {
        _externals.RemoveWhere(x => x.Type == ExternalService.Microsoft && x.Identity == identity);

        UpdateActivation();
    }

    public void AddRole(string role) =>
        _roles = _roles.Append(role).Distinct().ToArray();

    public void DeleteRole(string role) =>
        _roles = _roles.Where(x => x != role).ToArray();

    public void Ban(string reason, DateTime expiredAt, TimeProvider timeProvider)
    {
        _ban = new Ban(reason, expiredAt, timeProvider.UtcNow());

        AddDomainEvent(new AccountBannedDomainEvent(Id, reason, expiredAt));
    }

    public void Unban()
    {
        _ban = null;

        AddDomainEvent(new AccountUnbannedDomainEvent(Id));
    }

    public void Update(string? firstName, string? lastName) =>
        Update(Name.Nickname, firstName, lastName, Name.PreferNickname);

    public void Update(string nickname, string? firstName, string? lastName, bool preferNickname)
    {
        Name = new Name(nickname, firstName, lastName, preferNickname);

        AddDomainEvent(new AccountUpdatedDomainEvent(Id));
    }

    public void Update(Uri? picture)
    {
        Picture = picture?.ToString() ?? DefaultPicture;

        AddDomainEvent(new AccountUpdatedDomainEvent(Id));
    }

    public void Update(Language language, RegionCode region, CountryCode country, TimeZone timeZone)
    {
        RegionCode = region;
        CountryCode = country;
        Language = language;
        TimeZone = timeZone;

        if (_regCountryCode == CountryCode.Empty)
            _regCountryCode = CountryCode;

        AddDomainEvent(new AccountUpdatedDomainEvent(Id));
    }

    public void Update(DateFormat dateFormat, TimeFormat timeFormat, DayOfWeek weekStart)
    {
        DateFormat = dateFormat;
        TimeFormat = timeFormat;
        StartOfWeek = weekStart;

        AddDomainEvent(new AccountUpdatedDomainEvent(Id));
    }

    private void UpdateActivation() =>
        IsActivated = _externals.Count > 0 || _emails.Any(x => x is { IsPrimary: true, IsConfirmed: true });
}
