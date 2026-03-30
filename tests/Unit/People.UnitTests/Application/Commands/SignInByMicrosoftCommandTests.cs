using System.Net;
using System.Net.Mail;
using NSubstitute;
using People.Api.Application.Commands.SignInByMicrosoft;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.ValueObjects;
using People.Kafka.Integration;
using Xunit;

namespace People.UnitTests.Application.Commands;

public sealed class SignInByMicrosoftCommandTests
{
    private static MicrosoftAccount MsProfile(string identity = "ms-88") =>
        new(identity, new MailAddress("m@example.com"), "M", "User");

    private static ExternalSignInMatch Match(AccountId id) =>
        new(id, Name.Create("m", "M", "User", preferNickname: false));

    [Fact]
    public async Task Handle_LinkedMicrosoftIdentity_ReturnsSignInResultAndPublishesLoggedInEvent()
    {
        var accountId = new AccountId(402L);
        var microsoft = Substitute.For<IMicrosoftApiService>();
        microsoft.GetAsync("ms-access", Arg.Any<CancellationToken>()).Returns(MsProfile());

        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(ExternalService.Microsoft, "ms-88", Arg.Any<CancellationToken>()).Returns(Match(accountId));

        var bus = Substitute.For<IIntegrationEventBus>();

        var handler = new SignInByMicrosoftCommandHandler(bus, repo, microsoft);

        var result = await handler.Handle(
            new SignInByMicrosoftCommand("ms-access", IPAddress.Loopback, null),
            CancellationToken.None);

        Assert.Equal(accountId, result.Id);
        Assert.Equal("M User", result.FullName);
        await microsoft.Received(1).GetAsync("ms-access", Arg.Any<CancellationToken>());
        await bus.Received(1).PublishAsync(
            Arg.Is<AccountActivity.LoggedInIntegrationEvent>(e => e.AccountId == (long)accountId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MicrosoftIdentityNotLinked_ThrowsExternalNotFound()
    {
        var microsoft = Substitute.For<IMicrosoftApiService>();
        microsoft.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(MsProfile("unlinked"));

        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(ExternalService.Microsoft, "unlinked", Arg.Any<CancellationToken>()).Returns((ExternalSignInMatch?)null);

        var bus = Substitute.For<IIntegrationEventBus>();

        var handler = new SignInByMicrosoftCommandHandler(bus, repo, microsoft);

        var ex = await Assert.ThrowsAsync<ExternalAccountException>(async () =>
            await handler.Handle(new SignInByMicrosoftCommand("t", IPAddress.Loopback, null), CancellationToken.None));

        Assert.Equal(nameof(ExternalAccountException.NotFound), ex.Code);
        await bus.DidNotReceive()
            .PublishAsync(Arg.Any<AccountActivity.LoggedInIntegrationEvent>(), Arg.Any<CancellationToken>());
    }
}
