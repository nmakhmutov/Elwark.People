//using System.Net;
//using System.Threading;
//using System.Threading.Tasks;
//using Elwark.People.Api.Application.Commands;
//using Elwark.People.Api.Error;
//using Elwark.People.Domain.AggregatesModel.AccountAggregate;
//using Elwark.People.Domain.Exceptions;
//using Elwark.People.Domain.ReadModel.Account;
//using Elwark.People.Infrastructure;
//using Elwark.Security;
//using Moq;
//using Xunit;
//
//namespace Elwark.People.Api.UnitTests.Commands
//{
//    public class RegisterInternalAccountCommandTest
//    {
//        private readonly Mock<IAccountRepository> _accountRepository;
//        private readonly Mock<IAccountDataProvider> _dataProvider;
//        private readonly Mock<IPasswordHasher> _passwordHasher;
//
//        public RegisterInternalAccountCommandTest()
//        {
//            var context = new Mock<OAuthContext>();
//            context.Setup(authContext => authContext.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
//                .ReturnsAsync(true);
//
//            _accountRepository = new Mock<IAccountRepository>();
//            _accountRepository.Setup(repository =>
//                    repository.CreateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
//                .ReturnsAsync(It.IsAny<Account>);
//            _accountRepository.Setup(repository => repository.UnitOfWork)
//                .Returns(() => context.Object);
//
//            _dataProvider = new Mock<IAccountDataProvider>();
//
//            _passwordHasher = new Mock<IPasswordHasher>();
//            _passwordHasher.Setup(hasher => hasher.GenerateSaltBytes())
//                .Returns(() => new byte[] {1, 2, 3});
//            _passwordHasher.Setup(hasher => hasher.HashPasswordToBytes(It.IsAny<string>(), It.IsAny<byte[]>()))
//                .Returns(() => new byte[] {1, 2, 3});
//        }
//
//        [Fact]
//        public async Task Handle_internal_account_success()
//        {
//            var email = "test@email.com";
//            var confirmUrl = "http://confirm.url?code={code}";
//            var marker = "{code}";
//            string password = "password";
//            string userAgent = "agent";
//            IPAddress ip = IPAddress.Any;
//
//            _dataProvider.Setup(x => x.FindIdAsync(It.IsAny<string>(), CancellationToken.None))
//                .ReturnsAsync(() => null);
//            
//            var command = new RegisterInternalAccountCommand(
//                email,
//                password,
//                confirmUrl,
//                marker,
//                ip,
//                userAgent
//            );
//            
//            var handler = new RegisterInternalAccountCommandHandler(_accountRepository.Object, _passwordHasher.Object, _dataProvider.Object);
//            await handler.Handle(command, CancellationToken.None);
//        }
//        
//        [Fact]
//        public async Task Handle_internal_account_when_email_already_added()
//        {
//            var email = "test@email.com";
//            var confirmUrl = "http://confirm.url?code={code}";
//            var marker = "{code}";
//            string password = "password";
//            string userAgent = "agent";
//            IPAddress ip = IPAddress.Any;
//
//            _dataProvider.Setup(x => x.FindIdAsync(It.IsAny<string>(), CancellationToken.None))
//                .ReturnsAsync(1);
//            
//            var command = new RegisterInternalAccountCommand(
//                email,
//                password,
//                confirmUrl,
//                marker,
//                ip,
//                userAgent
//            );
//            
//            var handler = new RegisterInternalAccountCommandHandler(_accountRepository.Object, _passwordHasher.Object, _dataProvider.Object);
//
//            var result = await Assert.ThrowsAsync<ElwarkException>(() => handler.Handle(command, CancellationToken.None));
//            var error = OAuthErrors.EmailAlreadyRegistered(email);
//            
//            Assert.Equal(error.Code, result.Code);
//            Assert.Equal(error.Type, result.Type);
//            Assert.Equal(error.Message, result.Message);
//        }
//    }
//}