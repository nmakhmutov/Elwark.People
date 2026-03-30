using System.Net;
using System.Net.Mail;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using People.Api.Application.Commands.SignUpByGoogle;
using People.Api.Infrastructure.Providers.Google;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.IntegrationTests.Infrastructure;
using Xunit;

namespace People.IntegrationTests.Commands;

public sealed class GoogleSignUpFlowTests(PostgreSqlFixture postgres) : CommandIntegrationTestBase(postgres)
{
    [Fact]
    public async Task SignUpByGoogle_PersistsGoogleConnectionAndEmail()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        var googleEmail = new MailAddress("google.user@example.com");
        Commands.Google.GetAsync("google-access-token", Arg.Any<CancellationToken>())
            .Returns(new GoogleAccount(
                "google-subject-1",
                googleEmail,
                true,
                "Go",
                "User",
                null,
                null));

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(
            new SignUpByGoogleCommand("google-access-token", Language.Parse("en"), IPAddress.Loopback, null),
            CancellationToken.None);

        await using var read = Commands.CreateReadOnlyContext();

        var connection = await read.Connections.AsNoTracking()
            .SingleOrDefaultAsync(c =>
                EF.Property<AccountId>(c, "_accountId") == result.Id && c.Type == ExternalService.Google);
        Assert.NotNull(connection);
        Assert.Equal("google-subject-1", connection.Identity);

        var emailRow = await read.Emails.AsNoTracking().SingleAsync(e => e.AccountId == result.Id);
        Assert.Equal(googleEmail.Address, emailRow.Email);
        Assert.True(emailRow.IsConfirmed);
    }
}
