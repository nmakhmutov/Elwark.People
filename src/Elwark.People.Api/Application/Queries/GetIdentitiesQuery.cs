using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Shared;
using MediatR;

namespace Elwark.People.Api.Application.Queries
{
    public class GetIdentitiesQuery : IRequest<IReadOnlyCollection<IdentityResponse>>
    {
        [DebuggerStepThrough]
        public GetIdentitiesQuery(AccountId id) => AccountId = id;

        public AccountId AccountId { get; }
    }

    public class GetIdentitiesQueryHandler : IRequestHandler<GetIdentitiesQuery, IReadOnlyCollection<IdentityResponse>>
    {
        private const string Sql = @"
SELECT id, account_id, identification_type, notification_type, value, confirmed_at, created_at
FROM identities
WHERE account_id = @id;
";

        private readonly IDatabaseQueryExecutor _executor;

        public GetIdentitiesQueryHandler(IDatabaseQueryExecutor executor) =>
            _executor = executor;

        public Task<IReadOnlyCollection<IdentityResponse>> Handle(GetIdentitiesQuery request,
            CancellationToken cancellationToken) =>
            _executor.MultiplyAsync(Sql,
                new Dictionary<string, object>
                {
                    {"@id", request.AccountId.Value}
                },
                reader => new IdentityResponse(
                    new IdentityId(reader.GetFieldValue<Guid>(0)),
                    new AccountId(reader.GetFieldValue<long>(1)),
                    Identification.Create(reader.GetFieldValue<int>(2), reader.GetFieldValue<string>(4)),
                    Notification.Create(reader.GetFieldValue<int>(3), reader.GetFieldValue<string>(4)),
                    reader.GetFieldValue<DateTimeOffset?>(5),
                    reader.GetFieldValue<DateTimeOffset>(6)
                ),
                cancellationToken);
    }
}