using System.Net.Mail;
using NSubstitute;
using People.Application.Commands.SignInByMicrosoft;
using People.Application.Providers.Microsoft;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using Xunit;

namespace Unit.Api.Tests.Application.Commands;

public sealed class SignInByMicrosoftCommandTests
{
    private static readonly DateTime Utc = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static MicrosoftAccount MsProfile(string identity = "ms-88") =>
        new(identity, new MailAddress("m@example.com"), "M", "User");

    [Fact]
    public async Task Handle_LinkedMicrosoftIdentity_ReturnsSignInResult()
    {
        var accountId = new AccountId(402L);
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(accountId, time);

        var microsoft = Substitute.For<IMicrosoftApiService>();
        microsoft.GetAsync("ms-access", Arg.Any<CancellationToken>()).Returns(MsProfile());

        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);

        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(ExternalService.Microsoft, "ms-88", Arg.Any<CancellationToken>()).Returns(account);
        repo.UnitOfWork.Returns(uow);

        var handler = new SignInByMicrosoftCommandHandler(repo, microsoft, time);

        var result = await handler.Handle(
            new SignInByMicrosoftCommand("ms-access"),
            CancellationToken.None);

        Assert.Equal(accountId, result.Id);
        await microsoft.Received(1).GetAsync("ms-access", Arg.Any<CancellationToken>());
        await repo.Received(1).GetAsync(ExternalService.Microsoft, "ms-88", Arg.Any<CancellationToken>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MicrosoftIdentityNotLinked_ThrowsExternalNotFound()
    {
        var microsoft = Substitute.For<IMicrosoftApiService>();
        microsoft.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(MsProfile("unlinked"));

        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(ExternalService.Microsoft, "unlinked", Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new SignInByMicrosoftCommandHandler(repo, microsoft, TimeProvider.System);

        var ex = await Assert.ThrowsAsync<ExternalAccountException>(async () =>
            await handler.Handle(new SignInByMicrosoftCommand("t"), CancellationToken.None));

        Assert.Equal(nameof(ExternalAccountException.NotFound), ex.Code);
    }
}
