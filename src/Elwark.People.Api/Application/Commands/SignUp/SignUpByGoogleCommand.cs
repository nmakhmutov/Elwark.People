using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Api.Infrastructure.Services.Google;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.Storage.Client;
using MediatR;

namespace Elwark.People.Api.Application.Commands.SignUp
{
    public class SignUpByGoogleCommand : IRequest<SignUpResponse>
    {
        public SignUpByGoogleCommand(Identification.Google google, Identification.Email email, string accessToken)
        {
            Google = google;
            Email = email;
            AccessToken = accessToken;
        }

        public Identification.Google Google { get; }
        public Identification.Email Email { get; }
        public string AccessToken { get; }
    }

    public class SignUpByGoogleCommandHandler : IRequestHandler<SignUpByGoogleCommand, SignUpResponse>
    {
        private readonly IGoogleApiService _google;
        private readonly IAccountRepository _repository;
        private readonly IElwarkStorageClient _storage;
        private readonly IIdentificationValidator _validator;

        public SignUpByGoogleCommandHandler(IElwarkStorageClient storage, IAccountRepository repository,
            IGoogleApiService google, IIdentificationValidator validator)
        {
            _storage = storage;
            _repository = repository;
            _google = google;
            _validator = validator;
        }

        public async Task<SignUpResponse> Handle(SignUpByGoogleCommand request, CancellationToken cancellationToken)
        {
            var google = await _google.GetAsync(request.AccessToken, cancellationToken);

            if (google.Id != request.Google)
                throw new ElwarkGoogleException(GoogleError.IdMismatch);

            var account = new Account(
                new Name(request.Email.GetUser(), google.FirstName, google.LastName),
                google.Locale ?? CultureInfo.CurrentCulture,
                google.Picture ?? _storage.Static.Icons.User.Default.Path
            );

            var isEmailConfirmed = google.IsEmailVerified && request.Email == google.Email;
            
            await account.AddIdentificationAsync(request.Email, isEmailConfirmed, _validator);
            await account.AddIdentificationAsync(request.Google, true, _validator);
            
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