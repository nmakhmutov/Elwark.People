using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Shared;
using MediatR;

namespace Elwark.People.Api.Application.Queries
{
    public class GetIdentityByIdQuery : IRequest<IdentityModel>
    {
        [DebuggerStepThrough]
        public GetIdentityByIdQuery(IdentityId identityId) =>
            IdentityId = identityId;

        public IdentityId IdentityId { get; }
    }

    public class GetIdentityByIdQueryHandler : IRequestHandler<GetIdentityByIdQuery, IdentityModel?>
    {
        private const string Sql = @"
SELECT id, account_id, identification_type, notification_type, value, confirmed_at, created_at
FROM identities
WHERE id = @id;
";

        private readonly IDatabaseQueryExecutor _executor;

        public GetIdentityByIdQueryHandler(IDatabaseQueryExecutor executor) =>
            _executor = executor;

        public Task<IdentityModel?> Handle(GetIdentityByIdQuery request, CancellationToken cancellationToken) =>
            _executor.SingleAsync(Sql,
                new Dictionary<string, object>
                {
                    {"@id", request.IdentityId.Value}
                },
                reader => new IdentityModel(
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