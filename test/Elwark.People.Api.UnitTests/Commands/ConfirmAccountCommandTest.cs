//using System;
//using System.Collections.Generic;
//using System.Net;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Elwark.People.Api.Application.Commands;
//using Elwark.People.Api.Application.Confirmation;
//using Elwark.People.Api.Error;
//using Elwark.People.Domain.AggregatesModel.AccountAggregate;
//using Elwark.People.Domain.Exceptions;
//using Elwark.People.Infrastructure;
//using Elwark.People.Infrastructure.Confirmation;
//using Moq;
//using Xunit;
//
//namespace Elwark.People.Api.UnitTests.Commands
//{
//    public class ConfirmAccountCommandTest
//    {
//        private readonly Mock<IAccountRepository> _accountRepository;
//        private readonly Mock<IConfirmationManager> _confirmationManager;
//
//        public ConfirmAccountCommandTest()
//        {
//            var context = new Mock<OAuthContext>();
//            context.Setup(authContext => authContext.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
//                .ReturnsAsync(true);
//            
//            _accountRepository = new Mock<IAccountRepository>();
//            _accountRepository.Setup(repository => repository.Update(It.IsAny<Account>()))
//                .Returns(It.IsAny<Account>);
//            _accountRepository.Setup(repository => repository.UnitOfWork)
//                .Returns(() => context.Object);
//            
//            _confirmationManager = new Mock<IConfirmationManager>();
//            _confirmationManager.Setup(repository => repository.Delete(It.IsAny<ConfirmationModel>()))
//                .Returns(It.IsAny<ConfirmationModel>());
//        }
//
//        [Theory, MemberData(nameof(Accounts))]
//        public async Task Handle_confirm_account_success(Account account)
//        {
//            var confirmation = Guid.NewGuid();
//            var expiration = DateTimeOffset.Now.AddDays(1);
//            var command = new ConfirmIdentityCommand(It.IsAny<long>(), confirmation);
//
//            _accountRepository.Setup(repository => repository.FindAsync(It.IsAny<long>(), CancellationToken.None))
//                .ReturnsAsync(account);
//
//            _confirmationManager.Setup(repository => repository.FindAsync(confirmation, CancellationToken.None))
//                .ReturnsAsync(
//                    new ConfirmationModel(
//                        It.IsAny<long>(),
//                        ConfirmationType.ConfirmIdentity,
//                        ConfirmationMethod.Email,
//                        expiration)
//                );
//
//            var handler = new ConfirmIdentityCommandHandler(_accountRepository.Object, _confirmationManager.Object);
//            var cltToken = new CancellationToken();
//
//            await handler.Handle(command, cltToken);
//        }
//        
//        [Theory, MemberData(nameof(Accounts))]
//        public async Task Throw_when_conformation_type_incorrect(Account account)
//        {
//            var confirmation = Guid.NewGuid();
//            var expiration = DateTimeOffset.Now.AddDays(1);
//            var wrongConfirmationType = ConfirmationType.ChangeEmail;
//            
//            var command = new ConfirmIdentityCommand(It.IsAny<long>(), confirmation);
//
//            _accountRepository.Setup(repository => repository.FindAsync(It.IsAny<long>(), CancellationToken.None))
//                .ReturnsAsync(account);
//
//            _confirmationManager.Setup(repository => repository.FindAsync(confirmation, CancellationToken.None))
//                .ReturnsAsync(
//                    new ConfirmationModel(
//                        It.IsAny<long>(),
//                        wrongConfirmationType,
//                        ConfirmationMethod.Email,
//                        expiration)
//                );
//            
//            var handler = new ConfirmIdentityCommandHandler(_accountRepository.Object, _confirmationManager.Object);
//            var cltToken = new CancellationToken();
//
//            var result = await Assert.ThrowsAsync<ElwarkException>(() => handler.Handle(command, cltToken));
//            var error = OAuthErrors.ConfirmationNotMatch();
//            
//            Assert.Equal(error.Type, result.Type);
//            Assert.Equal(error.Code, result.Code);
//        }
//        
//        [Theory, MemberData(nameof(Accounts))]
//        public async Task Throw_when_account_already_confirmed(Account account)
//        {
//            account.ConfirmEmailAndActivateAccount();
//            
//            var confirmation = Guid.NewGuid();
//            var expiration = DateTimeOffset.Now.AddDays(1);
//            var command = new ConfirmIdentityCommand(It.IsAny<long>(), confirmation);
//
//            _accountRepository.Setup(repository => repository.FindAsync(It.IsAny<long>(), CancellationToken.None))
//                .ReturnsAsync(account);
//
//            _confirmationManager.Setup(repository => repository.FindAsync(confirmation, CancellationToken.None))
//                .ReturnsAsync(
//                    new ConfirmationModel(
//                        It.IsAny<long>(),
//                        ConfirmationType.ConfirmIdentity,
//                        ConfirmationMethod.Email,
//                        expiration)
//                );
//            
//            var handler = new ConfirmIdentityCommandHandler(_accountRepository.Object, _confirmationManager.Object);
//            var cltToken = new CancellationToken();
//
//            var result = await Assert.ThrowsAsync<ElwarkException>(() => handler.Handle(command, cltToken));
//            var error = OAuthErrors.EmailAlreadyConfirmed(account.Email.Value);
//            
//            Assert.Equal(error.Type, result.Type);
//            Assert.Equal(error.Code, result.Code);
//        }
//        
//        [Fact]
//        public async Task Throw_when_account_not_found()
//        {
//            var confirmation = Guid.NewGuid();
//            var expiration = DateTimeOffset.Now.AddDays(1);
//            var command = new ConfirmIdentityCommand(It.IsAny<long>(), confirmation);
//
//            _accountRepository.Setup(repository => repository.FindAsync(It.IsAny<long>(), CancellationToken.None))
//                .ReturnsAsync(() => null);
//
//            _confirmationManager.Setup(repository => repository.FindAsync(confirmation, CancellationToken.None))
//                .ReturnsAsync(
//                    new ConfirmationModel(
//                        It.IsAny<long>(),
//                        ConfirmationType.ConfirmIdentity,
//                        ConfirmationMethod.Email,
//                        expiration)
//                );
//            
//            var handler = new ConfirmIdentityCommandHandler(_accountRepository.Object, _confirmationManager.Object);
//            var cltToken = new CancellationToken();
//
//            var result = await Assert.ThrowsAsync<ElwarkException>(() => handler.Handle(command, cltToken));
//            var error = OAuthErrors.AccountNotFound(It.IsAny<long>());
//            
//            Assert.Equal(error.Type, result.Type);
//            Assert.Equal(error.Code, result.Code);
//        }
//        
//        [Theory, MemberData(nameof(Accounts))]
//        public async Task Throw_when_confirmation_not_found(Account account)
//        {
//            var confirmation = Guid.NewGuid();
//            var expiration = DateTimeOffset.Now.AddDays(1);
//            var command = new ConfirmIdentityCommand(It.IsAny<long>(), confirmation);
//
//            _accountRepository.Setup(repository => repository.FindAsync(It.IsAny<long>(), CancellationToken.None))
//                .ReturnsAsync(account);
//
//            _confirmationManager.Setup(repository => repository.FindAsync(confirmation, CancellationToken.None))
//                .ReturnsAsync(() => null);
//            
//            var handler = new ConfirmIdentityCommandHandler(_accountRepository.Object, _confirmationManager.Object);
//            var cltToken = new CancellationToken();
//
//            var result = await Assert.ThrowsAsync<ElwarkException>(() => handler.Handle(command, cltToken));
//            var error = OAuthErrors.ConfirmationNotFound();
//            
//            Assert.Equal(error.Type, result.Type);
//            Assert.Equal(error.Code, result.Code);
//        }
//        
//        [Theory, MemberData(nameof(Accounts))]
//        public async Task Throw_when_confirmation_expired(Account account)
//        {
//            var confirmation = Guid.NewGuid();
//            var expiration = DateTimeOffset.Now.AddDays(-1);
//            var command = new ConfirmIdentityCommand(It.IsAny<long>(), confirmation);
//
//            _accountRepository.Setup(repository => repository.FindAsync(It.IsAny<long>(), CancellationToken.None))
//                .ReturnsAsync(account);
//
//            _confirmationManager.Setup(repository => repository.FindAsync(confirmation, CancellationToken.None))
//                .ReturnsAsync(
//                    new ConfirmationModel(
//                        It.IsAny<long>(),
//                        ConfirmationType.ConfirmIdentity,
//                        ConfirmationMethod.Email,
//                        expiration)
//                );
//            
//            var handler = new ConfirmIdentityCommandHandler(_accountRepository.Object, _confirmationManager.Object);
//            var cltToken = new CancellationToken();
//
//            var result = await Assert.ThrowsAsync<ElwarkException>(() => handler.Handle(command, cltToken));
//            var error = OAuthErrors.ConfirmationExpired();
//            
//            Assert.Equal(error.Type, result.Type);
//            Assert.Equal(error.Code, result.Code);
//        }
//        
//        public static IEnumerable<object[]> Accounts => new[]
//        {
//            new object[]
//            {
//                new Account("test@elwark.com",
//                    Encoding.UTF8.GetBytes("password"),
//                    Encoding.UTF8.GetBytes("salt"),
//                    IPAddress.Loopback,
//                    "Agent"
//                )
//            }
//        };
//    }
//}