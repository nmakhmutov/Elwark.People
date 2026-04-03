using System.Net.Mail;
using Mediator;
using NSubstitute;
using People.Application.Commands.SignInByEmail;
using People.Application.Commands.SigningInByEmail;
using People.Domain.ValueObjects;
using People.Infrastructure;
using Xunit;

namespace Integration.Api.Tests.Commands;

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
                CancellationToken.None,
                nickname: "login-user"
            );
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
            new SignInByEmailCommand(token, signInCode!),
            CancellationToken.None);

        Assert.Equal("login-user", signIn.FullName);
    }
}
