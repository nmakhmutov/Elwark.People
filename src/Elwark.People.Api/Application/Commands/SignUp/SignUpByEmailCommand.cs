using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.Storage.Client;
using MediatR;

namespace Elwark.People.Api.Application.Commands.SignUp
{
    public class SignUpByEmailCommand : IRequest<SignUpResponse>
    {
        public SignUpByEmailCommand(Identification.Email email, string password)
        {
            Email = email;
            Password = password;
        }

        public Identification.Email Email { get; }

        public string Password { get; }
    }

    public class SignUpByEmailCommandHandler : IRequestHandler<SignUpByEmailCommand, SignUpResponse>
    {
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAccountRepository _repository;
        private readonly IElwarkStorageClient _storage;
        private readonly IPasswordValidator _passwordValidator;
        private readonly IIdentificationValidator _identificationValidator;

        public SignUpByEmailCommandHandler(IAccountRepository repository, IPasswordHasher hasher,
            IElwarkStorageClient storage, IPasswordValidator passwordValidator, IIdentificationValidator identificationValidator)
        {
            _repository = repository;
            _passwordHasher = hasher;
            _storage = storage;
            _passwordValidator = passwordValidator;
            _identificationValidator = identificationValidator;
        }

        public async Task<SignUpResponse> Handle(SignUpByEmailCommand request, CancellationToken cancellationToken)
        {
            var img = _storage.Static.Icons.User.Default.Path;
            var account = new Account(new Name(request.Email.GetUser()), CultureInfo.CurrentCulture, img);
            await account.AddIdentificationAsync(request.Email, _identificationValidator);
            await account.SetPasswordAsync(request.Password, _passwordValidator, _passwordHasher);

            await _repository.CreateAsync(account, cancellationToken);
            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            return new SignUpResponse(
                account.Id,
                account.Name.Nickname,
                account.Identities
                    .Select(x =>
                        new RegistrationIdentityResponse(x.Id, x.Identification, x.Notification,
                            x.ConfirmedAt.HasValue))
                    .ToArray()
            );
        }
    }
}