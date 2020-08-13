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
    public class GetIdentityByIdentifierQuery : IRequest<IdentityModel?>
    {
        [DebuggerStepThrough]
        public GetIdentityByIdentifierQuery(Identification identification) =>
            Identification = identification;

        public Identification Identification { get; }
    }

    public class GetIdentityByIdentifierQueryHandler : IRequestHandler<GetIdentityByIdentifierQuery, IdentityModel?>
    {
        private const string Sql = @"
SELECT id, account_id, identification_type, notification_type, value, confirmed_at, created_at
FROM identities
WHERE identification_type = @type AND value = @value;
";

        private readonly IDatabaseQueryExecutor _executor;

        public GetIdentityByIdentifierQueryHandler(IDatabaseQueryExecutor executor) =>
            _executor = executor;

        public Task<IdentityModel?> Handle(GetIdentityByIdentifierQuery request,
            CancellationToken cancellationToken) =>
            _executor.SingleAsync(Sql,
                new Dictionary<string, object>
                {
                    {"@type", (int) request.Identification.Type},
                    {"@value", request.Identification.Value}
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