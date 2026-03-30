using System.Net;
using NSubstitute;
using People.Api.Application.Commands.SignInByEmail;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.ValueObjects;
using People.Infrastructure.Confirmations;
using People.Kafka.Integration;
using Xunit;

namespace People.UnitTests.Application.Commands;

public sealed class SignInByEmailCommandTests
{
    private static readonly AccountId AccountId = new(302L);

    private static ExternalSignInMatch Match() =>
        new(AccountId, Name.Create("nick", "Pat", "Lee", preferNickname: false));

    [Fact]
    public async Task Handle_ValidConfirmation_ReturnsSignInResultAndPublishesLoggedInEvent()
    {
        var confirmation = Substitute.For<IConfirmationService>();
        confirmation
            .SignInAsync("tok", "9999", Arg.Any<CancellationToken>())
            .Returns(AccountId);

        var repo = Substitute.For<IAccountRepository>();
        repo.GetSignInMatchAsync(AccountId, Arg.Any<CancellationToken>()).Returns(Match());

        var bus = Substitute.For<IIntegrationEventBus>();

        var handler = new SignInByEmailCommandHandler(bus, confirmation, repo);

        var result = await handler.Handle(
            new SignInByEmailCommand("tok", "9999", IPAddress.Loopback, null),
            CancellationToken.None);

        Assert.Equal(AccountId, result.Id);
        Assert.Equal("Pat Lee", result.FullName);
        await bus.Received(1).PublishAsync(
            Arg.Is<AccountActivity.LoggedInIntegrationEvent>(e => e.AccountId == (long)AccountId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConfirmationVerificationFails_DoesNotPublishOrLoadAccount()
    {
        var confirmation = Substitute.For<IConfirmationService>();
        confirmation
            .SignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AccountId>(new InvalidOperationException("invalid")));

        var repo = Substitute.For<IAccountRepository>();
        var bus = Substitute.For<IIntegrationEventBus>();

        var handler = new SignInByEmailCommandHandler(bus, confirmation, repo);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handle(
                new SignInByEmailCommand("x", "y", IPAddress.Loopback, null),
                CancellationToken.None));

        await repo.DidNotReceive().GetSignInMatchAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
        await bus.DidNotReceive()
            .PublishAsync(Arg.Any<AccountActivity.LoggedInIntegrationEvent>(), Arg.Any<CancellationToken>());
    }
}
