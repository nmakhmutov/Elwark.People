using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Shared;
using MediatR;

namespace Elwark.People.Api.Application.Queries
{
    public class CheckPasswordAvailabilityQuery : IRequest<bool>
    {
        public CheckPasswordAvailabilityQuery(AccountId accountId) =>
            AccountId = accountId;

        public AccountId AccountId { get; }
    }

    public class CheckPasswordAvailabilityQueryHandler : IRequestHandler<CheckPasswordAvailabilityQuery, bool>
    {
        private const string Sql = @"
SELECT created_at
FROM passwords
WHERE account_id = @id;
";

        private readonly IDatabaseQueryExecutor _executor;

        public CheckPasswordAvailabilityQueryHandler(IDatabaseQueryExecutor executor) =>
            _executor = executor;

        public Task<bool> Handle(CheckPasswordAvailabilityQuery request, CancellationToken cancellationToken) =>
            _executor.SingleAsync(Sql,
                new Dictionary<string, object>
                {
                    {"@id", request.AccountId.Value}
                },
                _ => true,
                () => false,
                cancellationToken);
    }
}