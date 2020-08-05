using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Shared;
using MediatR;

namespace Elwark.People.Api.Application.Queries.SignIn
{
    public class SignInByFacebookQuery : IRequest<SignInResponse>
    {
        public SignInByFacebookQuery(Identification.Facebook facebook, string accessToken)
        {
            Facebook = facebook;
            AccessToken = accessToken;
        }

        public Identification.Facebook Facebook { get; }

        public string AccessToken { get; }
    }

    public class SignInByFacebookQueryHandler : SignInByExternalProviderQueryHandler<SignInByFacebookQuery>
    {
        public SignInByFacebookQueryHandler(IDatabaseQueryExecutor executor)
            : base(executor)
        {
        }

        public override async Task<SignInResponse> Handle(SignInByFacebookQuery request,
            CancellationToken cancellationToken)
        {
            var db = await GetDbData(request.Facebook, cancellationToken);
            db.Validate();

            return new SignInResponse(db.AccountId, db.IdentityId);
        }
    }
}