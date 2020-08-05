using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Shared;
using MediatR;

namespace Elwark.People.Api.Application.Queries.SignIn
{
    public class SignInByMicrosoftQuery : IRequest<SignInResponse>
    {
        public SignInByMicrosoftQuery(Identification.Microsoft microsoft, string accessToken)
        {
            Microsoft = microsoft;
            AccessToken = accessToken;
        }

        public Identification.Microsoft Microsoft { get; }

        public string AccessToken { get; }
    }

    public class SignInByMicrosoftQueryHandler : SignInByExternalProviderQueryHandler<SignInByMicrosoftQuery>
    {
        public SignInByMicrosoftQueryHandler(IDatabaseQueryExecutor executor)
            : base(executor)
        {
        }

        public override async Task<SignInResponse> Handle(SignInByMicrosoftQuery request,
            CancellationToken cancellationToken)
        {
            var db = await GetDbData(request.Microsoft, cancellationToken);
            db.Validate();

            return new SignInResponse(db.AccountId, db.IdentityId);
        }
    }
}