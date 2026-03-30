using System.Net.Mail;
using NSubstitute;
using People.Api.Application.Commands.AppendMicrosoft;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using Xunit;

namespace People.UnitTests.Application.Commands;

public sealed class AppendMicrosoftCommandTests
{
    private static readonly AccountId AccountId = new(504L);
    private static readonly DateTime Utc = new(2026, 7, 3, 11, 0, 0, DateTimeKind.Utc);

    private static MicrosoftAccount Profile(string identity, string email) =>
        new(identity, new MailAddress(email), "M", "Extra");

    [Fact]
    public async Task Handle_AppendsMicrosoftIdentityAndEmailWhenNotAlreadyRegistered()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time, "local@example.com");

        var microsoft = Substitute.For<IMicrosoftApiService>();
        microsoft.GetAsync("ms-tok", Arg.Any<CancellationToken>()).Returns(Profile("ms-append", "mextra@example.com"));

        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);
        repo.IsExistsAsync(ExternalService.Microsoft, "ms-append", Arg.Any<CancellationToken>()).Returns(false);
        repo.IsExistsAsync(Arg.Is<MailAddress>(m => m.Address == "mextra@example.com"), Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new AppendMicrosoftCommandHandler(microsoft, repo, time);

        await handler.Handle(new AppendMicrosoftCommand(AccountId, "ms-tok"), CancellationToken.None);

        Assert.Contains(account.Externals, e => e.Type == ExternalService.Microsoft && e.Identity == "ms-append");
        Assert.Contains(account.Emails, e => e.Email == "mextra@example.com");
        await microsoft.Received(1).GetAsync("ms-tok", Arg.Any<CancellationToken>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AccountMissing_ThrowsNotFound()
    {
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new AppendMicrosoftCommandHandler(
            Substitute.For<IMicrosoftApiService>(),
            repo,
            EmailHandlerTestAccounts.FixedTime(Utc));

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(new AppendMicrosoftCommand(AccountId, "t"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MicrosoftIdentityAlreadyRegisteredGlobally_SkipsAddMicrosoft()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(AccountId, time);

        var microsoft = Substitute.For<IMicrosoftApiService>();
        microsoft.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Profile("ms-taken", "other@example.com"));

        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);
        repo.IsExistsAsync(ExternalService.Microsoft, "ms-taken", Arg.Any<CancellationToken>()).Returns(true);
        repo.IsExistsAsync(Arg.Any<MailAddress>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = new AppendMicrosoftCommandHandler(microsoft, repo, time);

        await handler.Handle(new AppendMicrosoftCommand(AccountId, "t"), CancellationToken.None);

        Assert.DoesNotContain(account.Externals, e => e.Identity == "ms-taken");
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }
}
