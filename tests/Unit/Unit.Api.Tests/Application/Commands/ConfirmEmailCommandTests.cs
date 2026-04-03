using System.Net.Mail;
using NSubstitute;
using People.Application.Commands.ConfirmEmail;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Application.Providers.Confirmation;
using Xunit;

namespace Unit.Api.Tests.Application.Commands;

public sealed class ConfirmEmailCommandTests
{
    private static readonly AccountId AccountId = new(300L);
    private static readonly DateTime Utc = new(2026, 5, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ValidToken_ReturnsConfirmedEmailAndSaves()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var pendingAddr = new MailAddress("pending@test.com");
        var account = EmailHandlerTestAccounts.AccountWithUnconfirmedExtra(AccountId, time, pending: pendingAddr.Address);
        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var confirmation = Substitute.For<IConfirmationChallengeService>();
        var tokens = Substitute.For<IEmailVerificationTokenService>();
        var confirmationId = Guid.Parse("11111111-1111-7111-8111-111111111111");
        tokens.ParseToken("tok").Returns(new EmailVerificationTokenPayload(confirmationId, pendingAddr));
        confirmation
            .VerifyAsync(
                Convert.ToBase64String(confirmationId.ToByteArray()),
                "123456",
                ConfirmationType.EmailConfirmation,
                Arg.Any<CancellationToken>())
            .Returns(AccountId);

        var handler = new ConfirmEmailCommandHandler(confirmation, tokens, time, repo);

        var result = await handler.Handle(new ConfirmEmailCommand("tok", "123456"), CancellationToken.None);

        Assert.Equal(pendingAddr.Address, result.Email);
        Assert.True(result.IsConfirmed);
        tokens.Received(1).ParseToken("tok");
        await confirmation.Received(1).VerifyAsync(
            Convert.ToBase64String(confirmationId.ToByteArray()),
            "123456",
            ConfirmationType.EmailConfirmation,
            Arg.Any<CancellationToken>());
        await repo.Received(1).GetAsync(AccountId, Arg.Any<CancellationToken>());
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VerificationFails_PropagatesException()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var repo = Substitute.For<IAccountRepository>();
        repo.UnitOfWork.Returns(Substitute.For<IUnitOfWork>());

        var confirmation = Substitute.For<IConfirmationChallengeService>();
        var tokens = Substitute.For<IEmailVerificationTokenService>();
        tokens.ParseToken(Arg.Any<string>()).Returns(new EmailVerificationTokenPayload(Guid.NewGuid(), new MailAddress("bad@test.com")));
        confirmation
            .VerifyAsync(Arg.Any<string>(), Arg.Any<string>(), ConfirmationType.EmailConfirmation, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AccountId>(new InvalidOperationException("bad token")));

        var handler = new ConfirmEmailCommandHandler(confirmation, tokens, time, repo);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handle(new ConfirmEmailCommand("bad", "bad"), CancellationToken.None));

        await repo.DidNotReceive().GetAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_CallsSaveEntitiesAsync()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var pendingAddr = new MailAddress("p2@test.com");
        var account = EmailHandlerTestAccounts.AccountWithUnconfirmedExtra(AccountId, time, pending: pendingAddr.Address);
        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var confirmation = Substitute.For<IConfirmationChallengeService>();
        var tokens = Substitute.For<IEmailVerificationTokenService>();
        var confirmationId = Guid.Parse("22222222-2222-7222-8222-222222222222");
        tokens.ParseToken(Arg.Any<string>()).Returns(new EmailVerificationTokenPayload(confirmationId, pendingAddr));
        confirmation
            .VerifyAsync(
                Convert.ToBase64String(confirmationId.ToByteArray()),
                Arg.Any<string>(),
                ConfirmationType.EmailConfirmation,
                Arg.Any<CancellationToken>())
            .Returns(AccountId);

        var handler = new ConfirmEmailCommandHandler(confirmation, tokens, time, repo);

        await handler.Handle(new ConfirmEmailCommand("t", "c"), CancellationToken.None);

        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }
}
