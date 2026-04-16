using System.Net.Mail;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using People.Application.Commands.SignUpByEmail;
using People.Application.Commands.SigningUpByEmail;
using People.Domain.ValueObjects;
using People.Infrastructure;
using Xunit;

namespace Integration.Api.Tests.Commands;

public sealed class EmailSignUpFlowTests(PostgreSqlFixture postgres) : CommandIntegrationTestBase(postgres)
{
    [Fact]
    public async Task SigningUpByEmail_ThenSignUpByEmail_CreatesConfirmedPrimaryEmail()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        string? capturedCode = null;
        Commands.Notification
            .SendConfirmationAsync(
                Arg.Any<MailAddress>(),
                Arg.Do<string>(c => capturedCode = c),
                Arg.Any<Locale>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.CompletedTask);

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var email = new MailAddress("new-user@example.com");
        var token = await sender.Send(
            new SigningUpByEmailCommand(
                email,
                Locale.Parse("en"),
                Timezone.Utc,
                System.Net.IPAddress.Loopback
            ),
            CancellationToken.None
        );

        Assert.False(string.IsNullOrEmpty(token));
        Assert.NotNull(capturedCode);

        var signUp = await sender.Send(
            new SignUpByEmailCommand(token, capturedCode!),
            CancellationToken.None);

        await using var read = Commands.CreateReadOnlyContext();
        var row = await read.Emails.AsNoTracking().SingleOrDefaultAsync(e => e.AccountId == signUp.Id);
        Assert.NotNull(row);
        Assert.Equal(email.Address, row.Email);
        Assert.True(row.IsPrimary);
        Assert.True(row.IsConfirmed);

        await Commands.Notification
            .Received(1)
            .SendConfirmationAsync(email, Arg.Any<string>(), Locale.Parse("en"), Arg.Any<CancellationToken>());
    }
}
