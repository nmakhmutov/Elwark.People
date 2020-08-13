using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Shared;
using MediatR;

namespace Elwark.People.Api.Application.Queries.SignIn
{
    public class SignInByMicrosoftQuery : IRequest<SignInModel>
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

        public override async Task<SignInModel> Handle(SignInByMicrosoftQuery request,
            CancellationToken cancellationToken)
        {
            var db = await GetDbData(request.Microsoft, cancellationToken);
            db.Validate();

            return new SignInModel(db.AccountId, db.IdentityId);
        }
    }
}