using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Api.Infrastructure.Services.Microsoft;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.Storage.Client;
using MediatR;

namespace Elwark.People.Api.Application.Commands.SignUp
{
    public class SignUpByMicrosoftCommand : IRequest<SignUpResponse>
    {
        public SignUpByMicrosoftCommand(Identification.Microsoft microsoft, Identification.Email email,
            string accessToken)
        {
            Microsoft = microsoft;
            Email = email;
            AccessToken = accessToken;
        }

        public Identification.Microsoft Microsoft { get; }
        public Identification.Email Email { get; }
        public string AccessToken { get; }
    }

    public class SignUpByMicrosoftCommandHandler : IRequestHandler<SignUpByMicrosoftCommand, SignUpResponse>
    {
        private readonly IMicrosoftApiService _microsoft;
        private readonly IAccountRepository _repository;
        private readonly IElwarkStorageClient _storage;
        private readonly IIdentificationValidator _validator;

        public SignUpByMicrosoftCommandHandler(IElwarkStorageClient storage, IAccountRepository repository,
            IMicrosoftApiService microsoft, IIdentificationValidator validator)
        {
            _storage = storage;
            _repository = repository;
            _microsoft = microsoft;
            _validator = validator;
        }

        public async Task<SignUpResponse> Handle(SignUpByMicrosoftCommand request, CancellationToken cancellationToken)
        {
            var microsoft = await _microsoft.GetAsync(request.AccessToken, cancellationToken);

            if (microsoft.Id != request.Microsoft)
                throw new ElwarkMicrosoftException(MicrosoftError.IdMismatch);

            var account = new Account(
                new Name(request.Email.GetUser(), microsoft.FirstName, microsoft.LastName),
                CultureInfo.CurrentCulture,
                _storage.Static.Icons.User.Default.Path
            );
            await account.AddIdentificationAsync(request.Email, request.Email == microsoft.Email, _validator);
            await account.AddIdentificationAsync(request.Microsoft, true, _validator);

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