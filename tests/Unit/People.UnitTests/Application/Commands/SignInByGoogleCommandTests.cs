using System.Globalization;
using System.Net;
using System.Net.Mail;
using NSubstitute;
using People.Api.Application.Commands.SignInByGoogle;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Infrastructure.Providers.Google;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.ValueObjects;
using People.Kafka.Integration;
using Xunit;

namespace People.UnitTests.Application.Commands;

public sealed class SignInByGoogleCommandTests
{
    private static GoogleAccount GoogleProfile(string identity = "gid-99") =>
        new(
            identity,
            new MailAddress("g@example.com"),
            isEmailVerified: true,
            firstName: "G",
            lastName: "User",
            picture: null,
            locale: new CultureInfo("en"));

    private static ExternalSignInMatch Match(AccountId id) =>
        new(id, Name.Create("g", "G", "User", preferNickname: false));

    [Fact]
    public async Task Handle_LinkedGoogleIdentity_ReturnsSignInResultAndPublishesLoggedInEvent()
    {
        var accountId = new AccountId(401L);
        var google = Substitute.For<IGoogleApiService>();
        google.GetAsync("access", Arg.Any<CancellationToken>()).Returns(GoogleProfile());

        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(ExternalService.Google, "gid-99", Arg.Any<CancellationToken>()).Returns(Match(accountId));

        var bus = Substitute.For<IIntegrationEventBus>();

        var handler = new SignInByGoogleCommandHandler(bus, repo, google);

        var result = await handler.Handle(new SignInByGoogleCommand("access", IPAddress.Loopback, null), CancellationToken.None);

        Assert.Equal(accountId, result.Id);
        Assert.Equal("G User", result.FullName);
        await google.Received(1).GetAsync("access", Arg.Any<CancellationToken>());
        await bus.Received(1).PublishAsync(
            Arg.Is<AccountActivity.LoggedInIntegrationEvent>(e => e.AccountId == (long)accountId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GoogleIdentityNotLinked_ThrowsExternalNotFound()
    {
        var google = Substitute.For<IGoogleApiService>();
        google.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(GoogleProfile("orphan"));

        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(ExternalService.Google, "orphan", Arg.Any<CancellationToken>()).Returns((ExternalSignInMatch?)null);

        var bus = Substitute.For<IIntegrationEventBus>();

        var handler = new SignInByGoogleCommandHandler(bus, repo, google);

        var ex = await Assert.ThrowsAsync<ExternalAccountException>(async () =>
            await handler.Handle(new SignInByGoogleCommand("t", IPAddress.Loopback, null), CancellationToken.None));

        Assert.Equal(nameof(ExternalAccountException.NotFound), ex.Code);
        await bus.DidNotReceive()
            .PublishAsync(Arg.Any<AccountActivity.LoggedInIntegrationEvent>(), Arg.Any<CancellationToken>());
    }
}
