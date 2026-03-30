using System.Net;
using NSubstitute;
using People.Api.Application.DomainEventHandlers;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain.DomainEvents;
using People.Domain.Entities;
using People.Kafka.Integration;
using People.UnitTests.Application.Commands;
using Xunit;

namespace People.UnitTests.Application.EventHandlers;

public sealed class AccountCreatedDomainEventHandlerTests
{
    [Fact]
    public async Task Handle_PublishesAccountCreatedIntegrationEvent_WithAccountIdAndIp()
    {
        var utc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var time = EmailHandlerTestAccounts.FixedTime(utc);
        var accountId = new AccountId(901L);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(accountId, time);
        var ip = IPAddress.Parse("198.51.100.22");

        var bus = Substitute.For<IIntegrationEventBus>();

        var sut = new AccountCreatedDomainEventHandler(bus);
        await sut.Handle(new AccountCreatedDomainEvent(account, ip), CancellationToken.None);

        await bus.Received(1).PublishAsync(
            Arg.Is<AccountCreatedIntegrationEvent>(e =>
                e.AccountId == 901L && e.Ip == "198.51.100.22"),
            Arg.Any<CancellationToken>());
    }
}
