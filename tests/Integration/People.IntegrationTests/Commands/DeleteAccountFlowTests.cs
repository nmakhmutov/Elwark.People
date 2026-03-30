using System.Net.Mail;
using Mediator;
using Microsoft.EntityFrameworkCore;
using People.Api.Application.Commands.DeleteAccount;
using People.Domain.Entities;
using People.Infrastructure;
using People.IntegrationTests.Infrastructure;
using Xunit;

namespace People.IntegrationTests.Commands;

public sealed class DeleteAccountFlowTests(PostgreSqlFixture postgres) : CommandIntegrationTestBase(postgres)
{
    [Fact]
    public async Task DeleteAccountCommand_RemovesAccountAndEmails()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        using (var seedScope = Commands.CreateScope())
        {
            id = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("gone@example.com"),
                "gone",
                CancellationToken.None);
        }

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new DeleteAccountCommand(id), CancellationToken.None);

        await using var read = Commands.CreateReadOnlyContext();
        Assert.Null(await read.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id));
        Assert.Equal(0, await read.Emails.AsNoTracking().CountAsync(e => e.AccountId == id));
    }
}
