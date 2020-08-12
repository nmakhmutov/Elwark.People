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
//    public class RegisterExternalAccountCommandTest
//    {
//        private readonly Mock<IAccountRepository> _accountRepository;
//        private readonly Mock<IAccountDataProvider> _dataProvider;
//        private readonly Mock<IPasswordHasher> _passwordHasher;
//
//        public RegisterExternalAccountCommandTest()
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
//        }
//
//        [Fact]
//        public async Task Handle_external_account_success()
//        {
//            var email = "test@email.com";
//            var provider = new ExternalProvider(ExternalProviderType.Google, "123");
//            var accessToken = "token";
//            var confirmUrl = "http://confirm.url?code={code}";
//            var marker = "{code}";
//
//            var command = new RegisterExternalAccountCommand(
//                email,
//                provider,
//                accessToken,
//                confirmUrl,
//                marker,
//                IPAddress.Loopback,
//                "agent"
//            );
//
//            _dataProvider.Setup(dataProvider =>
//                    dataProvider.FindIdAsync(It.IsAny<string>(), CancellationToken.None))
//                .ReturnsAsync(() => null);
//
//            _dataProvider.Setup(dataProvider =>
//                    dataProvider.FindIdAsync(It.IsAny<ExternalProvider>(), CancellationToken.None))
//                .ReturnsAsync(() => null);
//
//            var handler = new RegisterExternalAccountCommandHandler(
//                _accountRepository.Object,
//                _passwordHasher.Object,
//                _dataProvider.Object);
//            var cltToken = new CancellationToken();
//
//            await handler.Handle(command, cltToken);
//        }
//
//        [Fact]
//        public async Task Throw_when_email_already_exist()
//        {
//            var email = "test@email.com";
//            var provider = new ExternalProvider(ExternalProviderType.Google, "123");
//            var accessToken = "token";
//            var confirmUrl = "http://elwark.com?code={code}";
//            var marker = "{code}";
//
//            var command = new RegisterExternalAccountCommand(
//                email,
//                provider,
//                accessToken,
//                confirmUrl,
//                marker,
//                IPAddress.Loopback,
//                "agent"
//            );
//
//            _dataProvider.Setup(dataProvider =>
//                    dataProvider.FindIdAsync(It.IsAny<string>(), CancellationToken.None))
//                .ReturnsAsync(() => It.IsAny<long>());
//
//            _dataProvider.Setup(dataProvider =>
//                    dataProvider.FindIdAsync(It.IsAny<ExternalProvider>(), CancellationToken.None))
//                .ReturnsAsync(() => null);
//
//            var handler = new RegisterExternalAccountCommandHandler(
//                _accountRepository.Object,
//                _passwordHasher.Object,
//                _dataProvider.Object);
//            var cltToken = new CancellationToken();
//
//            var result = await Assert.ThrowsAsync<ElwarkException>(() => handler.Handle(command, cltToken));
//            var error = OAuthErrors.EmailAlreadyRegistered(email);
//
//            Assert.Equal(error.Code, result.Code);
//            Assert.Equal(error.Type, result.Type);
//        }
//
//        [Fact]
//        public async Task Throw_when_external_provider_already_exist()
//        {
//            var email = "test@email.com";
//            var provider = new ExternalProvider(ExternalProviderType.Google, "123");
//            var accessToken = "token";
//            var confirmUrl = "http://confirm.url?code={code}";
//            var marker = "{code}";
//
//            var command = new RegisterExternalAccountCommand(
//                email,
//                provider,
//                accessToken,
//                confirmUrl,
//                marker,
//                IPAddress.Loopback,
//                "agent"
//            );
//
//            _dataProvider.Setup(dataProvider =>
//                    dataProvider.FindIdAsync(It.IsAny<string>(), CancellationToken.None))
//                .ReturnsAsync(() => null);
//
//            _dataProvider.Setup(dataProvider =>
//                    dataProvider.FindIdAsync(It.IsAny<ExternalProvider>(), CancellationToken.None))
//                .ReturnsAsync(() => It.IsAny<long>());
//
//            var handler = new RegisterExternalAccountCommandHandler(
//                _accountRepository.Object,
//                _passwordHasher.Object,
//                _dataProvider.Object);
//            var cltToken = new CancellationToken();
//
//            var result = await Assert.ThrowsAsync<ElwarkException>(() => handler.Handle(command, cltToken));
//            var error = OAuthErrors.IdentityAlreadyRegistered(provider.Type, provider.Id);
//
//            Assert.Equal(error.Code, result.Code);
//            Assert.Equal(error.Type, result.Type);
//        }
//    }
//}