using System.Net.Mail;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using People.Api.Application.Commands.AppendEmail;
using People.Api.Application.Commands.ChangePrimaryEmail;
using People.Api.Application.Commands.ConfirmEmail;
using People.Api.Application.Commands.ConfirmingEmail;
using People.Api.Application.Commands.DeleteEmail;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.IntegrationTests.Infrastructure;
using Xunit;

namespace People.IntegrationTests.Commands;

public sealed class EmailManagementFlowTests(PostgreSqlFixture postgres) : CommandIntegrationTestBase(postgres)
{
    [Fact]
    public async Task Append_Confirm_ChangePrimary_Delete_UpdatesDatabase()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        var primary = new MailAddress("primary@example.com");
        using (var seedScope = Commands.CreateScope())
        {
            id = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                primary,
                "multi-email",
                CancellationToken.None);
        }

        string? appendConfirmCode = null;
        Commands.Notification
            .SendConfirmationAsync(
                Arg.Any<MailAddress>(),
                Arg.Do<string>(c => appendConfirmCode = c),
                Arg.Any<Language>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.CompletedTask);

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var secondary = new MailAddress("secondary@example.com");
        _ = await sender.Send(new AppendEmailCommand(id, secondary), CancellationToken.None);

        await using (var read1 = Commands.CreateReadOnlyContext())
        {
            var rows = await read1.Emails.AsNoTracking().Where(e => e.AccountId == id).ToListAsync();
            Assert.Equal(2, rows.Count);
            Assert.Contains(rows, r => r.Email == secondary.Address && !r.IsConfirmed && !r.IsPrimary);
        }

        var confirming = await sender.Send(new ConfirmingEmailCommand(id, secondary), CancellationToken.None);

        Assert.NotNull(appendConfirmCode);
        _ = await sender.Send(new ConfirmEmailCommand(confirming.Token, appendConfirmCode!), CancellationToken.None);

        await using (var read2 = Commands.CreateReadOnlyContext())
        {
            var row = await read2.Emails.AsNoTracking().SingleAsync(e => e.AccountId == id && e.Email == secondary.Address);
            Assert.True(row.IsConfirmed);
        }

        await sender.Send(new ChangePrimaryEmailCommand(id, secondary), CancellationToken.None);

        await using (var read3 = Commands.CreateReadOnlyContext())
        {
            var primaryRow = await read3.Emails.AsNoTracking().SingleAsync(e => e.AccountId == id && e.IsPrimary);
            Assert.Equal(secondary.Address, primaryRow.Email);
        }

        await sender.Send(new DeleteEmailCommand(id, primary), CancellationToken.None);

        await using (var read4 = Commands.CreateReadOnlyContext())
        {
            var emails = await read4.Emails.AsNoTracking().Where(e => e.AccountId == id).ToListAsync();
            Assert.Single(emails);
            Assert.Equal(secondary.Address, emails[0].Email);
            Assert.True(emails[0].IsPrimary);
            Assert.True(emails[0].IsConfirmed);
        }
    }
}
