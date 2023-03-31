using System.Net;
using System.Net.Mail;
using People.Domain.DomainEvents;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using TimeZone = People.Domain.ValueObjects.TimeZone;

namespace People.Domain.Entities;

public sealed class Account : Entity<long>, IAggregateRoot
{
    private static readonly Uri DefaultPicture =
        new("https://res.cloudinary.com/elwark/image/upload/v1660058875/People/default.svg");

    private readonly List<EmailAccount> _emails;
    private readonly List<ExternalConnection> _externals;

    private Ban? _ban;
    private DateTime _createdAt;
    private Registration _registration;
    private string[] _roles;
    private DateTime _updatedAt;

    // ReSharper disable once UnusedMember.Local
    private Account()
    {
        Name = default!;
        Picture = DefaultPicture;
        _registration = default!;
        _emails = new List<EmailAccount>();
        _roles = Array.Empty<string>();
        _externals = new List<ExternalConnection>();
    }

    public Account(string nickname, Language language, Uri? picture, IPAddress ip, ITimeProvider time, IIpHasher hasher)
    {
        Name = new Name(nickname);
        Picture = picture ?? DefaultPicture;
        Language = language;
        CountryCode = CountryCode.Empty;
        TimeZone = TimeZone.Utc;
        TimeFormat = TimeFormat.Default;
        DateFormat = DateFormat.Default;
        StartOfWeek = DayOfWeek.Monday;
        IsActivated = false;
        _createdAt = _updatedAt = time.Now;
        _ban = null;
        _emails = new List<EmailAccount>();
        _roles = Array.Empty<string>();
        _externals = new List<ExternalConnection>();
        _registration = new Registration(hasher.CreateHash(ip), CountryCode.Empty);

        AddDomainEvent(new AccountCreatedDomainEvent(this, ip));
    }

    public Name Name { get; private set; }

    public Uri Picture { get; private set; }

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

    public DateTime GetCreatedDateTime() =>
        _createdAt;

    public DateTime GetUpdatedDateTime() =>
        _updatedAt;

    public void AddEmail(MailAddress email, bool isConfirmed, ITimeProvider time)
    {
        var notConfirmed = _emails.FirstOrDefault(x => !x.IsConfirmed);
        if (notConfirmed is not null)
            throw EmailException.NotConfirmed(new MailAddress(notConfirmed.Email));

        var now = time.Now;
        _updatedAt = now;
        _emails.Add(new EmailAccount(Id, email.Address, _emails.Count == 0, isConfirmed ? now : null, now));

        UpdateActivation();
    }

    public MailAddress GetPrimaryEmail() =>
        new(_emails.First(x => x.IsPrimary).Email);

    public void SetPrimaryEmail(MailAddress email, ITimeProvider time)
    {
        var result = _emails.FirstOrDefault(x => x.Email == email.Address);
        if (result is null)
            throw EmailException.NotFound(email);

        if (!result.IsConfirmed)
            throw EmailException.NotConfirmed(email);

        foreach (var item in _emails)
            item.RemovePrimary();

        result.SetPrimary();
        _updatedAt = time.Now;
    }

    public void ConfirmEmail(MailAddress email, ITimeProvider time)
    {
        var result = _emails.FirstOrDefault(x => x.Email == email.Address) ?? throw EmailException.NotFound(email);
        result.Confirm(time.Now);
        _updatedAt = time.Now;

        UpdateActivation();
        AddDomainEvent(new EmailConfirmedDomainEvent(this, email));
    }

    public void DeleteEmail(MailAddress email, ITimeProvider time)
    {
        var result = _emails.FirstOrDefault(x => x.Email == email.Address);
        if (result is null)
            return;

        if (result.IsPrimary)
            throw AccountException.PrimaryEmailCannotBeRemoved(Id);

        _emails.Remove(result);
        _updatedAt = time.Now;

        UpdateActivation();
    }

    public void AddGoogle(string identity, string? firstName, string? lastName, ITimeProvider time)
    {
        _updatedAt = time.Now;
        _externals.Add(ExternalConnection.Google(identity, firstName, lastName, _updatedAt));
        Name = new Name(Name.Nickname, Name.FirstName ?? firstName, Name.LastName ?? lastName, Name.PreferNickname);

        UpdateActivation();
        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void DeleteGoogle(string identity, ITimeProvider time)
    {
        _externals.RemoveAll(x => x.Type == ExternalService.Google && x.Identity == identity);
        _updatedAt = time.Now;

        UpdateActivation();
    }

    public void AddMicrosoft(string identity, string? firstName, string? lastName, ITimeProvider time)
    {
        _updatedAt = time.Now;
        _externals.Add(ExternalConnection.Microsoft(identity, firstName, lastName, _updatedAt));
        Name = new Name(Name.Nickname, Name.FirstName ?? firstName, Name.LastName ?? lastName, Name.PreferNickname);

        UpdateActivation();
        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void DeleteMicrosoft(string identity, ITimeProvider time)
    {
        _externals.RemoveAll(x => x.Type == ExternalService.Microsoft && x.Identity == identity);
        _updatedAt = time.Now;

        UpdateActivation();
    }

    public void AddRole(string role) =>
        _roles = _roles.Append(role).Distinct().ToArray();

    public void DeleteRole(string role) =>
        _roles = _roles.Where(x => x != role).ToArray();

    public void Ban(string reason, DateTime expiredAt, ITimeProvider time)
    {
        _updatedAt = time.Now;
        _ban = new Ban(reason, expiredAt, _updatedAt);

        AddDomainEvent(new AccountBannedDomainEvent(this, reason, expiredAt));
    }

    public void Unban(ITimeProvider time)
    {
        _ban = null;
        _updatedAt = time.Now;

        AddDomainEvent(new AccountUnbannedDomainEvent(this));
    }

    public void Update(string? firstName, string? lastName, ITimeProvider time) =>
        Update(Name.Nickname, firstName, lastName, Name.PreferNickname, time);

    public void Update(string nickname, string? firstName, string? lastName, bool preferNickname, ITimeProvider time)
    {
        Name = new Name(nickname, firstName, lastName, preferNickname);
        _updatedAt = time.Now;

        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void Update(Uri? picture, ITimeProvider time)
    {
        Picture = picture ?? DefaultPicture;
        _updatedAt = time.Now;

        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void Update(CountryCode country, ITimeProvider time)
    {
        CountryCode = country;
        _updatedAt = time.Now;

        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void Update(TimeZone timeZone, ITimeProvider time)
    {
        TimeZone = timeZone;
        _updatedAt = time.Now;

        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void Update(DateFormat dateFormat, ITimeProvider time)
    {
        DateFormat = dateFormat;
        _updatedAt = time.Now;

        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void Update(TimeFormat timeFormat, ITimeProvider time)
    {
        TimeFormat = timeFormat;
        _updatedAt = time.Now;

        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void Update(DayOfWeek weekStart, ITimeProvider time)
    {
        StartOfWeek = weekStart;
        _updatedAt = time.Now;

        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void Update(Language language, ITimeProvider time)
    {
        Language = language;
        _updatedAt = time.Now;

        AddDomainEvent(new AccountUpdatedDomainEvent(this));
    }

    public void UpdateRegistrationCountry(CountryCode code)
    {
        if (_registration.CountryCode != CountryCode.Empty)
            return;

        _registration = new Registration(_registration.Ip, code);
    }

    private void UpdateActivation() =>
        IsActivated = _externals.Count > 0 || _emails.Any(x => x.IsPrimary && x.IsConfirmed);
}
