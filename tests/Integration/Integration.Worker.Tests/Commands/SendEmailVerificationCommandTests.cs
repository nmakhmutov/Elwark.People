using System.Net.Mail;
using Integration.Shared.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using People.Application.Providers;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.Worker.Commands;
using Xunit;

namespace Integration.Worker.Tests.Commands;

[Collection(nameof(PostgresCollection))]
public sealed class SendEmailVerificationCommandTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Handle_CreatesConfirmationRecord_AndSendsNotification()
    {
        await using var db = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);

        var notification = Substitute.For<INotificationSender>();
        var handler = new SendEmailVerificationCommandHandler(db, notification);
        var command = new SendEmailVerificationCommand(
            600L,
            Guid.Parse("11111111-1111-7111-8111-111111111111"),
            "verify@test.com",
            "en",
            new DateTime(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc)
        );

        await handler.Handle(command, CancellationToken.None);

        var saved = await db.Confirmations.SingleAsync(x => x.Id == command.ConfirmationId);
        Assert.Equal(new AccountId(command.AccountId), saved.AccountId);
        Assert.Equal("EmailVerify", saved.Type);
        Assert.Equal(6, saved.Code.Length);
        Assert.Equal(command.OccurredAt.AddMinutes(30), saved.ExpiresAt);

        await notification.Received(1).SendConfirmationAsync(
            Arg.Is<MailAddress>(m => m.Address == command.Email),
            Arg.Is<string>(code => code.Length == 6),
            Arg.Is<Language>(l => l.ToString() == command.Language),
            Arg.Any<CancellationToken>());
    }
}
