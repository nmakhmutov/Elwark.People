using System.Net;
using System.Net.Mail;
using System.Reflection;
using NSubstitute;
using People.Domain.DomainEvents;
using People.Domain.Events;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using TimeZoneVo = People.Domain.ValueObjects.TimeZone;
using Xunit;

namespace Unit.Api.Tests.Domain.Entities;

public sealed class AccountTests
{
    private static readonly Picture ExpectedDefaultPicture =
        Picture.Parse("https://res.cloudinary.com/elwark/image/upload/v1/People/default.jpg");

    private static TimeProvider FakeTime(DateTime utc)
    {
        var tp = Substitute.For<TimeProvider>();
        tp.GetUtcNow().Returns(new DateTimeOffset(utc, TimeSpan.Zero));
        return tp;
    }

    private static Account CreateAccount(Language? language = null)
    {
        var hasher = Substitute.For<IIpHasher>();
        hasher.CreateHash(Arg.Any<IPAddress>()).Returns([1, 2, 3]);
        var time = FakeTime(new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        return Account.Create(language ?? Language.Parse("en"), IPAddress.Parse("127.0.0.1"), hasher, time);
    }

    private static string[] GetRoles(Account account) =>
        (string[])typeof(Account).GetField("_roles", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(account)!;

    private static void AssertSingle<T>(IReadOnlyCollection<IDomainEvent> events) where T : class, IDomainEvent
    {
        var matches = events.OfType<T>().ToList();
        Assert.Single(matches);
    }

    [Fact]
    public void Create_Defaults_MatchExpected()
    {
        var account = CreateAccount();

        Assert.Equal(ExpectedDefaultPicture, account.Picture);
        Assert.Equal(TimeZoneVo.Utc, account.TimeZone);
        Assert.Equal(DayOfWeek.Monday, account.StartOfWeek);
        Assert.False(account.IsActivated);
        Assert.Equal(DateFormat.Default, account.DateFormat);
        Assert.Equal(TimeFormat.Default, account.TimeFormat);
        Assert.Equal(RegionCode.Empty, account.Region);
        Assert.Equal(CountryCode.Empty, account.Country);
        Assert.Empty(GetRoles(account));
    }

    [Fact]
    public void Create_RaisesCreatedEvent()
    {
        var account = CreateAccount();
        AssertSingle<AccountCreatedDomainEvent>(account.GetDomainEvents());
        var evt = account.GetDomainEvents().OfType<AccountCreatedDomainEvent>().Single();
        Assert.Same(account, evt.Account);
        Assert.Equal(IPAddress.Parse("127.0.0.1"), evt.IpAddress);
        Assert.Equal(new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc), evt.OccurredAt);
    }

    [Fact]
    public void Create_CallsHasherWithIp()
    {
        var hasher = Substitute.For<IIpHasher>();
        var ip = IPAddress.Parse("10.0.0.5");
        hasher.CreateHash(ip).Returns([9, 9, 9]);

        var time = FakeTime(new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _ = Account.Create(Language.Parse("en"), ip, hasher, time);

        hasher.Received(1).CreateHash(ip);
    }

    [Fact]
    public void AddEmail_First_IsPrimary()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

        account.AddEmail(new MailAddress("a@b.co"), isConfirmed: false, time);

        var email = account.Emails.Single();
        Assert.True(email.IsPrimary);
        Assert.False(email.IsConfirmed);
    }

    [Fact]
    public void AddEmail_PendingUnconfirmed_Throws()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("first@x.com"), false, time);

        var ex = Assert.Throws<EmailException>(() =>
            account.AddEmail(new MailAddress("second@x.com"), false, time));

        Assert.Equal(new MailAddress("first@x.com").Address, ex.Email.Address);
    }

    [Fact]
    public void GetPrimaryEmail_ReturnsPrimaryAddress()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("primary@x.com"), true, time);

        Assert.Equal("primary@x.com", account.GetPrimaryEmail().Address);
    }

    [Fact]
    public void SetPrimaryEmail_Confirmed_Succeeds()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("a@x.com"), true, time);
        account.ClearDomainEvents();
        account.AddEmail(new MailAddress("b@x.com"), true, time);
        account.ClearDomainEvents();

        account.SetPrimaryEmail(new MailAddress("b@x.com"), time);

        Assert.True(account.Emails.Single(e => e.Email == "b@x.com").IsPrimary);
        Assert.False(account.Emails.Single(e => e.Email == "a@x.com").IsPrimary);
    }

    [Fact]
    public void SetPrimaryEmail_Unconfirmed_Throws()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("a@x.com"), true, time);
        account.AddEmail(new MailAddress("b@x.com"), false, time);

        Assert.Throws<EmailException>(() => account.SetPrimaryEmail(new MailAddress("b@x.com"), time));
    }

    [Fact]
    public void SetPrimaryEmail_Unknown_ThrowsNotFound()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("a@x.com"), true, time);

        Assert.Throws<EmailException>(() => account.SetPrimaryEmail(new MailAddress("missing@x.com"), time));
    }

    [Fact]
    public void ConfirmEmail_Existing_SetsConfirmed()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("a@x.com"), false, time);

        account.ConfirmEmail(new MailAddress("a@x.com"), time);

        Assert.True(account.Emails.Single().IsConfirmed);
    }

    [Fact]
    public void ConfirmEmail_Unknown_ThrowsNotFound()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("a@x.com"), true, time);

        Assert.Throws<EmailException>(() => account.ConfirmEmail(new MailAddress("nope@x.com"), time));
    }

    [Fact]
    public void DeleteEmail_Secondary_Removes()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("primary@x.com"), true, time);
        account.AddEmail(new MailAddress("extra@x.com"), true, time);

        account.DeleteEmail(new MailAddress("extra@x.com"), time);

        Assert.Single(account.Emails);
        Assert.Equal("primary@x.com", account.Emails.Single().Email);
    }

    [Fact]
    public void DeleteEmail_Primary_Throws()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("only@x.com"), true, time);

        Assert.Throws<AccountException>(() => account.DeleteEmail(new MailAddress("only@x.com"), time));
    }

    [Fact]
    public void DeleteEmail_Unknown_NoOp()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("a@x.com"), true, time);
        account.ClearDomainEvents();

        account.DeleteEmail(new MailAddress("ghost@x.com"), time);

        Assert.Empty(account.GetDomainEvents());
        Assert.Single(account.Emails);
    }

    [Fact]
    public void AddEmail_AfterClear_RaisesUpdated()
    {
        var account = CreateAccount();
        account.ClearDomainEvents();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        account.AddEmail(new MailAddress("a@x.com"), false, time);

        AssertSingle<AccountUpdatedDomainEvent>(account.GetDomainEvents());
    }

    [Fact]
    public void ConfirmPrimary_ActivatesAccount()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("a@x.com"), false, time);
        Assert.False(account.IsActivated);

        account.ConfirmEmail(new MailAddress("a@x.com"), time);

        Assert.True(account.IsActivated);
    }

    [Fact]
    public void AddGoogle_ActivatesAccount()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.False(account.IsActivated);

        account.AddGoogle("gid", null, null, null, time);

        Assert.True(account.IsActivated);
    }

    [Fact]
    public void RemoveGoogle_UnconfirmedPrimary_Deactivates()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("a@x.com"), false, time);
        account.AddGoogle("gid", null, null, null, time);
        Assert.True(account.IsActivated);

        account.DeleteGoogle("gid", time);

        Assert.False(account.IsActivated);
    }

    [Fact]
    public void AddGoogle_FillsMissingNames()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        account.AddGoogle("g1", "John", "Doe", null, time);

        var ext = account.Externals.Single();
        Assert.Equal(ExternalService.Google, ext.Type);
        Assert.Equal("g1", ext.Identity);
        Assert.Equal("John", account.Name.FirstName);
        Assert.Equal("Doe", account.Name.LastName);
    }

    [Fact]
    public void AddGoogle_RaisesUpdated()
    {
        var account = CreateAccount();
        account.ClearDomainEvents();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        account.AddGoogle("g1", null, null, null, time);

        AssertSingle<AccountUpdatedDomainEvent>(account.GetDomainEvents());
    }

    [Fact]
    public void DeleteGoogle_Known_Removes()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddGoogle("g1", null, null, null, time);

        account.DeleteGoogle("g1", time);

        Assert.Empty(account.Externals);
    }

    [Fact]
    public void DeleteGoogle_Unknown_NoChange()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddGoogle("g1", null, null, null, time);
        var before = account.Externals.Count;

        account.DeleteGoogle("unknown", time);

        Assert.Equal(before, account.Externals.Count);
    }

    [Fact]
    public void AddMicrosoft_AddsLink()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        account.AddMicrosoft("m1", "Jane", null, time);

        var ext = account.Externals.Single();
        Assert.Equal(ExternalService.Microsoft, ext.Type);
        Assert.Equal("m1", ext.Identity);
        Assert.Equal("Jane", account.Name.FirstName);
    }

    [Fact]
    public void DeleteMicrosoft_Known_Removes()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddMicrosoft("m1", null, null, time);

        account.DeleteMicrosoft("m1", time);

        Assert.Empty(account.Externals);
    }

    [Fact]
    public void DeleteMicrosoft_Unknown_NoChange()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddMicrosoft("m1", null, null, time);

        account.DeleteMicrosoft("x", time);

        Assert.Single(account.Externals);
    }

    [Fact]
    public void Update_PartialName_KeepsNickname()
    {
        var account = CreateAccount();
        var originalNickname = account.Name.Nickname;
        account.ClearDomainEvents();
        account.Update(
            Name.Create(account.Name.Nickname, "John", "Doe", account.Name.UseNickname),
            account.Picture,
            account.Language,
            account.Region,
            account.Country,
            account.TimeZone,
            account.DateFormat,
            account.TimeFormat,
            account.StartOfWeek,
            TimeProvider.System);

        Assert.Equal(originalNickname, account.Name.Nickname);
        Assert.Equal("John", account.Name.FirstName);
        Assert.Equal("Doe", account.Name.LastName);
        Assert.True(account.Name.UseNickname);
        AssertSingle<AccountUpdatedDomainEvent>(account.GetDomainEvents());
    }

    [Fact]
    public void Update_FullName_ReplacesName()
    {
        var account = CreateAccount();
        account.ClearDomainEvents();

        account.Update(
            Name.Create(Nickname.Parse("newnick"), "A", "B", useNickname: false),
            account.Picture,
            account.Language,
            account.Region,
            account.Country,
            account.TimeZone,
            account.DateFormat,
            account.TimeFormat,
            account.StartOfWeek,
            TimeProvider.System
        );

        Assert.Equal(Nickname.Parse("newnick"), account.Name.Nickname);
        Assert.Equal("A", account.Name.FirstName);
        Assert.Equal("B", account.Name.LastName);
        Assert.False(account.Name.UseNickname);
        AssertSingle<AccountUpdatedDomainEvent>(account.GetDomainEvents());
    }

    [Fact]
    public void Update_PictureNull_ResetsDefault()
    {
        var account = CreateAccount();
        account.ClearDomainEvents();
        var picture = Picture.Parse("https://example.com/p.png");

        account.Update(
            account.Name,
            picture,
            account.Language,
            account.Region,
            account.Country,
            account.TimeZone,
            account.DateFormat,
            account.TimeFormat,
            account.StartOfWeek,
            TimeProvider.System
        );

        Assert.Equal(picture, account.Picture);
        AssertSingle<AccountUpdatedDomainEvent>(account.GetDomainEvents());

        account.ClearDomainEvents();
        account.Update(
            account.Name,
            null,
            account.Language,
            account.Region,
            account.Country,
            account.TimeZone,
            account.DateFormat,
            account.TimeFormat,
            account.StartOfWeek,
            TimeProvider.System
        );
        Assert.Equal(ExpectedDefaultPicture, account.Picture);
        AssertSingle<AccountUpdatedDomainEvent>(account.GetDomainEvents());
    }

    [Fact]
    public void Update_Locale_UpdatesFields()
    {
        var account = CreateAccount();
        account.ClearDomainEvents();

        account.Update(
            account.Name,
            account.Picture,
            Language.Parse("ru"),
            RegionCode.Parse("EU"),
            CountryCode.Parse("DE"),
            TimeZoneVo.Parse("UTC"),
            account.DateFormat,
            account.TimeFormat,
            account.StartOfWeek,
            TimeProvider.System
        );

        Assert.Equal(Language.Parse("ru"), account.Language);
        Assert.Equal(RegionCode.Parse("EU"), account.Region);
        Assert.Equal(CountryCode.Parse("DE"), account.Country);
        Assert.Equal(TimeZoneVo.Parse("UTC"), account.TimeZone);
        AssertSingle<AccountUpdatedDomainEvent>(account.GetDomainEvents());

        account.ClearDomainEvents();
        account.Update(
            account.Name,
            account.Picture,
            Language.Parse("en"),
            RegionCode.Parse("NA"),
            CountryCode.Parse("US"),
            TimeZoneVo.Parse("UTC"),
            account.DateFormat,
            account.TimeFormat,
            account.StartOfWeek,
            TimeProvider.System
        );

        Assert.Equal(Language.Parse("en"), account.Language);
        Assert.Equal(RegionCode.Parse("NA"), account.Region);
        Assert.Equal(CountryCode.Parse("US"), account.Country);
        AssertSingle<AccountUpdatedDomainEvent>(account.GetDomainEvents());
    }

    [Fact]
    public void Update_Formats_UpdatesDisplayPrefs()
    {
        var account = CreateAccount();
        account.ClearDomainEvents();

        account.Update(
            account.Name,
            account.Picture,
            account.Language,
            account.Region,
            account.Country,
            account.TimeZone,
            DateFormat.Parse("dd.MM.yyyy"),
            TimeFormat.Parse("H:mm"),
            DayOfWeek.Tuesday,
            TimeProvider.System
        );

        Assert.Equal(DateFormat.Parse("dd.MM.yyyy"), account.DateFormat);
        Assert.Equal(TimeFormat.Parse("H:mm"), account.TimeFormat);
        Assert.Equal(DayOfWeek.Tuesday, account.StartOfWeek);
        AssertSingle<AccountUpdatedDomainEvent>(account.GetDomainEvents());
    }

    [Fact]
    public void AddRole_Duplicate_Ignored()
    {
        var account = CreateAccount();
        account.AddRole("adm", TimeProvider.System);
        account.AddRole("adm", TimeProvider.System);
        var roles = GetRoles(account);
        Assert.Single(roles);
        Assert.Equal("adm", roles[0]);
    }

    [Fact]
    public void AddRole_RaisesUpdated()
    {
        var account = CreateAccount();
        account.ClearDomainEvents();
        account.AddRole("r1", TimeProvider.System);
        AssertSingle<AccountUpdatedDomainEvent>(account.GetDomainEvents());
    }

    [Fact]
    public void DeleteRole_Known_Removes()
    {
        var account = CreateAccount();
        account.AddRole("a", TimeProvider.System);
        account.AddRole("b", TimeProvider.System);
        account.ClearDomainEvents();

        account.DeleteRole("a", TimeProvider.System);

        Assert.Equal(["b"], GetRoles(account));
        AssertSingle<AccountUpdatedDomainEvent>(account.GetDomainEvents());
    }

    [Fact]
    public void Ban_SetsState_RaisesBannedEvent()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        var expires = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);

        account.Ban("spam", expires, time);

        Assert.True(account.IsBanned);
        AssertSingle<AccountBannedDomainEvent>(account.GetDomainEvents());
        var evt = account.GetDomainEvents().OfType<AccountBannedDomainEvent>().Single();
        Assert.Equal("spam", evt.Reason);
        Assert.Equal(expires, evt.ExpiredAt);
    }

    [Fact]
    public void Unban_Clears_RaisesUnbannedEvent()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.Ban("x", DateTime.UtcNow.AddDays(1), time);
        account.ClearDomainEvents();

        account.Unban(time);

        Assert.False(account.IsBanned);
        AssertSingle<AccountUnbannedDomainEvent>(account.GetDomainEvents());
    }

    [Fact]
    public void Delete_RaisesDeletedEvent()
    {
        var account = CreateAccount();
        account.ClearDomainEvents();

        account.Delete(TimeProvider.System);

        AssertSingle<AccountDeletedDomainEvent>(account.GetDomainEvents());
    }

    [Fact]
    public void ClearDomainEvents_RemovesAll()
    {
        var account = CreateAccount();
        Assert.NotEmpty(account.GetDomainEvents());

        account.ClearDomainEvents();

        Assert.Empty(account.GetDomainEvents());
    }

    [Fact]
    public void ConfirmEmail_AfterClear_RaisesUpdated()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("a@x.com"), false, time);
        account.ClearDomainEvents();

        account.ConfirmEmail(new MailAddress("a@x.com"), time);

        AssertSingle<AccountUpdatedDomainEvent>(account.GetDomainEvents());
    }

    [Fact]
    public void SetPrimaryEmail_AfterClear_RaisesUpdated()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("a@x.com"), true, time);
        account.AddEmail(new MailAddress("b@x.com"), true, time);
        account.ClearDomainEvents();

        account.SetPrimaryEmail(new MailAddress("b@x.com"), time);

        AssertSingle<AccountUpdatedDomainEvent>(account.GetDomainEvents());
    }

    [Fact]
    public void DeleteEmail_Secondary_AfterClear_RaisesUpdated()
    {
        var account = CreateAccount();
        var time = FakeTime(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        account.AddEmail(new MailAddress("p@x.com"), true, time);
        account.AddEmail(new MailAddress("x@x.com"), true, time);
        account.ClearDomainEvents();

        account.DeleteEmail(new MailAddress("x@x.com"), time);

        AssertSingle<AccountUpdatedDomainEvent>(account.GetDomainEvents());
    }
}
