using System.Net;
using System.Net.Mail;
using Mediator;
using NSubstitute;
using People.Api.Application.Commands.SignInByEmail;
using People.Api.Application.Commands.SigningInByEmail;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.IntegrationTests.Infrastructure;
using Xunit;

namespace People.IntegrationTests.Commands;

public sealed class EmailSignInFlowTests(PostgreSqlFixture postgres) : CommandIntegrationTestBase(postgres)
{
    [Fact]
    public async Task SigningInByEmail_ThenSignInByEmail_PublishesLoggedInEvent()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        using (var seedScope = Commands.CreateScope())
        {
            _ = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("login@example.com"),
                "login-user",
                CancellationToken.None);
        }

        string? signInCode = null;
        Commands.Notification
            .SendConfirmationAsync(
                Arg.Any<MailAddress>(),
                Arg.Do<string>(c => signInCode = c),
                Arg.Any<Language>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.CompletedTask);

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var token = await sender.Send(
            new SigningInByEmailCommand(new MailAddress("login@example.com"), Language.Parse("en")),
            CancellationToken.None);

        Assert.NotNull(signInCode);

        var signIn = await sender.Send(
            new SignInByEmailCommand(token, signInCode!, IPAddress.Loopback, null),
            CancellationToken.None);

        Assert.Equal("login-user", signIn.FullName);

        await Commands.EventBus
            .Received(1)
            .PublishAsync(
                Arg.Is<AccountActivity.LoggedInIntegrationEvent>(e => e.AccountId == (long)signIn.Id),
                Arg.Any<CancellationToken>());
    }
}
