using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Shared;
using Elwark.People.Shared.Primitives;
using MediatR;

namespace Elwark.People.Api.Application.Queries
{
    public class GetBanQuery : IRequest<BanModel>
    {
        [DebuggerStepThrough]
        public GetBanQuery(AccountId accountId) =>
            AccountId = accountId;

        public AccountId AccountId { get; }
    }

    public class GetBanQueryHandler : IRequestHandler<GetBanQuery, BanModel>
    {
        private const string Sql = @"
SELECT b.type, b.created_at, b.expired_at, b.reason
FROM bans b
WHERE b.account_id = @id;
";

        private readonly IDatabaseQueryExecutor _executor;

        public GetBanQueryHandler(IDatabaseQueryExecutor executor) =>
            _executor = executor;

        public Task<BanModel> Handle(GetBanQuery request, CancellationToken cancellationToken) =>
            _executor.SingleAsync(Sql,
                new Dictionary<string, object>
                {
                    {"@id", request.AccountId.Value}
                },
                reader => new BanModel(true,
                    new BanDetail(
                        (BanType) reader.GetFieldValue<int>(0),
                        reader.GetFieldValue<DateTimeOffset>(1),
                        reader.GetFieldValue<DateTimeOffset?>(2),
                        reader.GetFieldValue<string>(3)
                    )
                ),
                () => new BanModel(false, null),
                cancellationToken);
    }
}