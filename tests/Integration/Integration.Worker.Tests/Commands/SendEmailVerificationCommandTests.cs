using System.Net.Mail;
using Integration.Shared.Tests.Infrastructure;
using NSubstitute;
using People.Application.Providers;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using People.Infrastructure.Confirmations;
using People.Worker.Commands;
using Xunit;

namespace Integration.Worker.Tests.Commands;

[Collection(nameof(PostgresCollection))]
public sealed class SendEmailVerificationCommandTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Handle_UsesExistingConfirmationRecord_AndSendsNotification()
    {
        await using var db = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);

        await db.Confirmations.AddAsync(
            new Confirmation(
                new AccountId(600L),
                "ABC123",
                ConfirmationType.EmailConfirmation,
                TimeSpan.FromMinutes(30), new DateTime(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None
        );
        await db.SaveChangesAsync(CancellationToken.None);

        var notification = Substitute.For<INotificationSender>();
        var handler = new SendEmailVerificationCommandHandler(db, notification);
        var command = new SendEmailVerificationCommand(600L, "verify@test.com", Locale.Parse("en"));

        await handler.Handle(command, CancellationToken.None);

        await notification.Received(1)
            .SendConfirmationAsync(
                Arg.Is<MailAddress>(m => m.Address == command.Email),
                "ABC123",
                Arg.Is<Locale>(x => x == command.Locale),
                Arg.Any<CancellationToken>()
            );
    }
}
