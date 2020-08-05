using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Shared;
using MediatR;

namespace Elwark.People.Api.Application.Queries.SignIn
{
    public class SignInByGoogleQuery : IRequest<SignInResponse>
    {
        public SignInByGoogleQuery(Identification.Google google, string accessToken)
        {
            Google = google;
            AccessToken = accessToken;
        }

        public Identification.Google Google { get; }

        public string AccessToken { get; }
    }

    public class SignInByGoogleQueryHandler : SignInByExternalProviderQueryHandler<SignInByGoogleQuery>
    {
        public SignInByGoogleQueryHandler(IDatabaseQueryExecutor executor)
            : base(executor)
        {
        }

        public override async Task<SignInResponse> Handle(SignInByGoogleQuery request,
            CancellationToken cancellationToken)
        {
            var db = await GetDbData(request.Google, cancellationToken);
            db.Validate();

            return new SignInResponse(db.AccountId, db.IdentityId);
        }
    }
}