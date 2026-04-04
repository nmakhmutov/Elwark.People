using System.Globalization;
using System.Net.Mail;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using People.Application.Commands.SignUpByMicrosoft;
using People.Application.Providers.Microsoft;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using People.Infrastructure;
using Xunit;

namespace Integration.Api.Tests.Commands;

public sealed class MicrosoftSignUpFlowTests(PostgreSqlFixture postgres) : CommandIntegrationTestBase(postgres)
{
    [Fact]
    public async Task SignUpByMicrosoft_PersistsAccountWithEmail()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        var msEmail = new MailAddress("ms.user@example.com");
        Commands.Microsoft.GetAsync("ms-access-token", Arg.Any<CancellationToken>())
            .Returns(new MicrosoftAccount("ms-oid-1", msEmail, "Ms", "Person"));

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(
            new SignUpByMicrosoftCommand(
                "ms-access-token",
                Language.Parse("en"),
                CultureInfo.InvariantCulture,
                System.Net.IPAddress.Loopback
            ),
            CancellationToken.None
        );

        await using var read = Commands.CreateReadOnlyContext();
        var emailRow = await read.Emails.AsNoTracking().SingleAsync(e => e.AccountId == result.Id);
        Assert.Equal(msEmail.Address, emailRow.Email);
        Assert.True(emailRow.IsConfirmed);
        Assert.True(emailRow.IsPrimary);

        var msConn = await read.Connections.AsNoTracking()
            .SingleOrDefaultAsync(c =>
                EF.Property<AccountId>(c, "_accountId") == result.Id && c.Type == ExternalService.Microsoft);
        Assert.NotNull(msConn);
        Assert.Equal("ms-oid-1", msConn.Identity);
    }
}
