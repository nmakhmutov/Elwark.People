using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Api.Infrastructure.Services.Facebook;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Shared.Primitives;
using Elwark.Storage.Client;
using MediatR;

namespace Elwark.People.Api.Application.Commands.SignUp
{
    public class SignUpByFacebookCommand : IRequest<SignUpModel>
    {
        public SignUpByFacebookCommand(Identification.Facebook facebook, Identification.Email email, string accessToken)
        {
            Facebook = facebook;
            Email = email;
            AccessToken = accessToken;
        }

        public Identification.Facebook Facebook { get; }
        public Identification.Email Email { get; }
        public string AccessToken { get; }
    }

    public class SignUpByFacebookCommandHandler : IRequestHandler<SignUpByFacebookCommand, SignUpModel>
    {
        private readonly IFacebookApiService _facebook;
        private readonly IAccountRepository _repository;
        private readonly IElwarkStorageClient _storage;
        private readonly IIdentificationValidator _validator;
        
        public SignUpByFacebookCommandHandler(IElwarkStorageClient storage, IAccountRepository repository,
            IFacebookApiService facebook, IIdentificationValidator validator)
        {
            _storage = storage;
            _repository = repository;
            _facebook = facebook;
            _validator = validator;
        }

        public async Task<SignUpModel> Handle(SignUpByFacebookCommand request, CancellationToken cancellationToken)
        {
            var facebook = await _facebook.GetAsync(request.AccessToken, cancellationToken);
            if (facebook.Id != request.Facebook)
                throw new ElwarkFacebookException(FacebookError.IdMismatch);

            var account = new Account(
                new Name(request.Email.GetUser(), facebook.FirstName, facebook.LastName),
                new BasicInfo(CultureInfo.CurrentCulture,
                    facebook.Gender ?? Gender.Female,
                    birthday: facebook.Birthday
                ),
                facebook.Picture ?? _storage.Static.Icons.User.Default.Path,
                new Links
                {
                    [LinksType.Facebook] = facebook.Link
                }
            );
            await account.AddIdentificationAsync(request.Email, _validator);
            await account.AddIdentificationAsync(request.Facebook, true, _validator);

            await _repository.CreateAsync(account, cancellationToken);
            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            return new SignUpModel(
                account.Id,
                account.Name.Nickname,
                account.Identities
                    .Select(x => new IdentityModel(
                        x.Id,
                        x.AccountId,
                        x.Identification,
                        x.Notification,
                        x.ConfirmedAt,
                        x.CreatedAt)
                    )
                    .ToArray()
            );
        }
    }
}