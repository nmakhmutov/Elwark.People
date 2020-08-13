using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Shared;
using MediatR;

namespace Elwark.People.Api.Application.Queries.SignIn
{
    public class SignInByGoogleQuery : IRequest<SignInModel>
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

        public override async Task<SignInModel> Handle(SignInByGoogleQuery request,
            CancellationToken cancellationToken)
        {
            var db = await GetDbData(request.Google, cancellationToken);
            db.Validate();

            return new SignInModel(db.AccountId, db.IdentityId);
        }
    }
}