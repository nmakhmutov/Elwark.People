using System.Net;
using System.Net.Mail;
using People.Domain.DomainEvents;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using TimeZone = People.Domain.ValueObjects.TimeZone;

namespace People.Domain.Entities;

// ReSharper disable NotAccessedField.Local
public sealed class Account : Entity<AccountId>, IAggregateRoot
{
    private const string DefaultPicture = "https://res.cloudinary.com/elwark/image/upload/v1/People/default.jpg";

    private readonly List<EmailAccount> _emails;
    private readonly List<ExternalConnection> _externals;

    private Ban? _ban;
    private DateTime _createdAt;
    private DateTime _lastLogIn;
    private CountryCode _regCountryCode;
    private byte[] _regIp;
    private string[] _roles;
    private DateTime _updatedAt;

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

    public bool IsBanned =>
        _ban is not null;

    public IReadOnlyCollection<EmailAccount> Emails =>
        _emails.ToArray();

    public IReadOnlyCollection<ExternalConnection> Externals =>
        _externals.ToArray();

#pragma warning disable CS8618
    // ReSharper disable once UnusedMember.Local
    private Account()
    {
    }
#pragma warning restore CS8618

    private Account(
        Name name,
        string picture,
        RegionCode regionCode,
        CountryCode countryCode,
        Language language,
        TimeZone timeZone,
        DateFormat dateFormat,
        TimeFormat timeFormat,
        DayOfWeek startOfWeek,
        bool isActivated,
        byte[] regIp
    )
    {
        Name = name;
        Picture = picture;
        RegionCode = regionCode;
        CountryCode = countryCode;
        Language = language;
        TimeZone = timeZone;
        DateFormat = dateFormat;
        TimeFormat = timeFormat;
        StartOfWeek = startOfWeek;
        IsActivated = isActivated;
        _regIp = regIp;
        _regCountryCode = CountryCode.Empty;
        _emails = [];
        _externals = [];
        _roles = [];
    }

    public void SetAsUpdated(TimeProvider provider)
    {
        if (_createdAt == DateTime.MinValue)
            _createdAt = provider.UtcNow();

        _updatedAt = provider.UtcNow();
    }

    public static Account Create(string nickname, Language language, IPAddress ip, IIpHasher hasher, TimeProvider time)
    {
        var account = new Account(
            Name.Create(nickname),
            DefaultPicture,
            RegionCode.Empty,
            CountryCode.Empty,
            language,
            TimeZone.Utc,
            DateFormat.Default,
            TimeFormat.Default,
            DayOfWeek.Monday,
            false,
            hasher.CreateHash(ip)
        );

        account.AddDomainEvent(new AccountCreatedDomainEvent(account, ip, time.UtcNow()));

        return account;
    }

    public DateTime GetCreatedDateTime() =>
        _createdAt;

    public void AddEmail(MailAddress email, bool isConfirmed, TimeProvider timeProvider)
    {
        var notConfirmed = _emails.FirstOrDefault(x => !x.IsConfirmed);
        if (notConfirmed is not null)
            throw EmailException.NotConfirmed(new MailAddress(notConfirmed.Email));

        var now = timeProvider.UtcNow();
        _emails.Add(EmailAccount.Create(Id, email.Address, _emails.Count == 0, isConfirmed ? now : null, now));

        UpdateActivation();
        AddDomainEvent(new AccountUpdatedDomainEvent(Id, now));
    }

    public MailAddress GetPrimaryEmail() =>
        new(_emails.First(x => x.IsPrimary).Email);

    public void SetPrimaryEmail(MailAddress email, TimeProvider timeProvider)
    {
        var result = _emails.FirstOrDefault(x => x.Email == email.Address) ?? throw EmailException.NotFound(email);

        if (!result.IsConfirmed)
            throw EmailException.NotConfirmed(email);

        foreach (var item in _emails)
            item.RemovePrimary();

        result.SetPrimary();
        AddDomainEvent(new AccountUpdatedDomainEvent(Id, timeProvider.UtcNow()));
    }

    public void ConfirmEmail(MailAddress email, TimeProvider timeProvider)
    {
        var result = _emails.FirstOrDefault(x => x.Email == email.Address) ?? throw EmailException.NotFound(email);
        var now = timeProvider.UtcNow();
        result.Confirm(now);

        UpdateActivation();
        AddDomainEvent(new AccountUpdatedDomainEvent(Id, now));
    }

    public void DeleteEmail(MailAddress email, TimeProvider timeProvider)
    {
        var result = _emails.FirstOrDefault(x => x.Email == email.Address);
        if (result is null)
            return;

        if (result.IsPrimary)
            throw AccountException.PrimaryEmailCannotBeRemoved(Id);

        _emails.Remove(result);

        UpdateActivation();
        AddDomainEvent(new AccountUpdatedDomainEvent(Id, timeProvider.UtcNow()));
    }

    public void AddGoogle(string identity, string? firstName, string? lastName, TimeProvider timeProvider)
    {
        var now = timeProvider.UtcNow();
        _externals.Add(ExternalConnection.Google(identity, firstName, lastName, now));
        Name = Name.Create(Name.Nickname, Name.FirstName ?? firstName, Name.LastName ?? lastName, Name.PreferNickname);

        UpdateActivation();
        AddDomainEvent(new AccountUpdatedDomainEvent(Id, now));
    }

    public void DeleteGoogle(string identity, TimeProvider timeProvider)
    {
        _externals.RemoveAll(x => x.Type == ExternalService.Google && x.Identity == identity);

        UpdateActivation();
        AddDomainEvent(new AccountUpdatedDomainEvent(Id, timeProvider.UtcNow()));
    }

    public void AddMicrosoft(string identity, string? firstName, string? lastName, TimeProvider timeProvider)
    {
        var now = timeProvider.UtcNow();
        _externals.Add(ExternalConnection.Microsoft(identity, firstName, lastName, now));
        Name = Name.Create(Name.Nickname, Name.FirstName ?? firstName, Name.LastName ?? lastName, Name.PreferNickname);

        UpdateActivation();
        AddDomainEvent(new AccountUpdatedDomainEvent(Id, now));
    }

    public void DeleteMicrosoft(string identity, TimeProvider timeProvider)
    {
        _externals.RemoveAll(x => x.Type == ExternalService.Microsoft && x.Identity == identity);

        UpdateActivation();
        AddDomainEvent(new AccountUpdatedDomainEvent(Id, timeProvider.UtcNow()));
    }

    public void AddRole(string role, TimeProvider timeProvider)
    {
        _roles = _roles.Append(role).Distinct().ToArray();
        AddDomainEvent(new AccountUpdatedDomainEvent(Id, timeProvider.UtcNow()));
    }

    public void DeleteRole(string role, TimeProvider timeProvider)
    {
        _roles = _roles.Where(x => x != role).ToArray();
        AddDomainEvent(new AccountUpdatedDomainEvent(Id, timeProvider.UtcNow()));
    }

    public void Ban(string reason, DateTime expiredAt, TimeProvider timeProvider)
    {
        var now = timeProvider.UtcNow();
        _ban = new Ban(reason, expiredAt, now);

        AddDomainEvent(new AccountBannedDomainEvent(Id, reason, expiredAt, now));
    }

    public void Unban(TimeProvider timeProvider)
    {
        _ban = null;

        AddDomainEvent(new AccountUnbannedDomainEvent(Id, timeProvider.UtcNow()));
    }

    public void Update(string? firstName, string? lastName, TimeProvider timeProvider) =>
        Update(Name.Nickname, firstName, lastName, Name.PreferNickname, timeProvider);

    public void Update(string nickname, string? firstName, string? lastName, bool preferNickname, TimeProvider timeProvider)
    {
        Name = Name.Create(nickname, firstName, lastName, preferNickname);

        AddDomainEvent(new AccountUpdatedDomainEvent(Id, timeProvider.UtcNow()));
    }

    public void Update(Uri? picture, TimeProvider timeProvider)
    {
        Picture = picture?.ToString() ?? DefaultPicture;

        AddDomainEvent(new AccountUpdatedDomainEvent(Id, timeProvider.UtcNow()));
    }

    public void Update(Language language, RegionCode region, CountryCode country, TimeZone timeZone, TimeProvider timeProvider)
    {
        RegionCode = region;
        CountryCode = country;
        Language = language;
        TimeZone = timeZone;

        if (_regCountryCode == CountryCode.Empty)
            _regCountryCode = CountryCode;

        AddDomainEvent(new AccountUpdatedDomainEvent(Id, timeProvider.UtcNow()));
    }

    public void Update(DateFormat dateFormat, TimeFormat timeFormat, DayOfWeek weekStart, TimeProvider timeProvider)
    {
        DateFormat = dateFormat;
        TimeFormat = timeFormat;
        StartOfWeek = weekStart;

        AddDomainEvent(new AccountUpdatedDomainEvent(Id, timeProvider.UtcNow()));
    }

    public void Delete(TimeProvider timeProvider) =>
        AddDomainEvent(new AccountDeletedDomainEvent(Id, timeProvider.UtcNow()));

    private void UpdateActivation() =>
        IsActivated = _externals.Count > 0 || _emails.Any(x => x is { IsPrimary: true, IsConfirmed: true });
}
