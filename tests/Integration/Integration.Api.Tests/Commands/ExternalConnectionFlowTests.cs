using System.Net.Mail;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using People.Application.Commands.AppendGoogle;
using People.Application.Commands.AppendMicrosoft;
using People.Application.Commands.DeleteGoogle;
using People.Application.Commands.DeleteMicrosoft;
using People.Application.Providers.Google;
using People.Application.Providers.Microsoft;
using People.Domain.Entities;
using People.Infrastructure;
using Xunit;

namespace Integration.Api.Tests.Commands;

public sealed class ExternalConnectionFlowTests(PostgreSqlFixture postgres) : CommandIntegrationTestBase(postgres)
{
    [Fact]
    public async Task Google_AppendThenDelete_UpdatesConnections()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        using (var seedScope = Commands.CreateScope())
        {
            id = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("g-ext@example.com"),
                CancellationToken.None
            );
        }

        var gEmail = new MailAddress("google-linked@example.com");
        Commands.Google.GetAsync("g-append-token", Arg.Any<CancellationToken>())
            .Returns(new GoogleAccount("g-identity-1", gEmail, true, "G", "L", null, null));

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new AppendGoogleCommand(id, "g-append-token"), CancellationToken.None);

        await using (var read1 = Commands.CreateReadOnlyContext())
        {
            var count = await read1.Connections.AsNoTracking()
                .CountAsync(c => EF.Property<AccountId>(c, "_accountId") == id && c.Type == ExternalService.Google);
            Assert.Equal(1, count);
        }

        await sender.Send(new DeleteGoogleCommand(id, "g-identity-1"), CancellationToken.None);

        await using (var read2 = Commands.CreateReadOnlyContext())
        {
            var count = await read2.Connections.AsNoTracking()
                .CountAsync(c => EF.Property<AccountId>(c, "_accountId") == id && c.Type == ExternalService.Google);
            Assert.Equal(0, count);
        }
    }

    [Fact]
    public async Task Microsoft_AppendThenDelete_UpdatesConnections()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        using (var seedScope = Commands.CreateScope())
        {
            id = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("m-ext@example.com"),
                CancellationToken.None
            );
        }

        var mEmail = new MailAddress("ms-linked@example.com");
        Commands.Microsoft.GetAsync("m-append-token", Arg.Any<CancellationToken>())
            .Returns(new MicrosoftAccount("m-identity-1", mEmail, "M", "L"));

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new AppendMicrosoftCommand(id, "m-append-token"), CancellationToken.None);

        await using (var read1 = Commands.CreateReadOnlyContext())
        {
            var count = await read1.Connections.AsNoTracking()
                .CountAsync(c => EF.Property<AccountId>(c, "_accountId") == id && c.Type == ExternalService.Microsoft);
            Assert.Equal(1, count);
        }

        await sender.Send(new DeleteMicrosoftCommand(id, "m-identity-1"), CancellationToken.None);

        await using (var read2 = Commands.CreateReadOnlyContext())
        {
            var count = await read2.Connections.AsNoTracking()
                .CountAsync(c => EF.Property<AccountId>(c, "_accountId") == id && c.Type == ExternalService.Microsoft);
            Assert.Equal(0, count);
        }
    }
}
