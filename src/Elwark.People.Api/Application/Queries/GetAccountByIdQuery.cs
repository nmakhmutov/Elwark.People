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
    public class GetAccountByIdQuery : IRequest<AccountModel>
    {
        [DebuggerStepThrough]
        public GetAccountByIdQuery(AccountId id) => Id = id;

        public AccountId Id { get; }
    }

    public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountModel?>
    {
        private const string Sql = @"
SELECT id,
       first_name,
       last_name,
       nickname,
       gender,
       birthday,
       country_code,
       language,
       city,
       timezone,
       bio,
       picture,
       roles,
       links,
       created_at,
       updated_at
FROM accounts
WHERE id = @id;
";

        private readonly IDatabaseQueryExecutor _executor;

        public GetAccountByIdQueryHandler(IDatabaseQueryExecutor executor) =>
            _executor = executor;

        public Task<AccountModel?> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken) =>
            _executor.SingleAsync(Sql,
                new Dictionary<string, object>
                {
                    {"@id", request.Id.Value}
                },
                reader => new AccountModel(
                    reader.GetFieldValue<long>(0),
                    reader.GetNullableFieldValue<string>(1),
                    reader.GetNullableFieldValue<string>(2),
                    reader.GetFieldValue<string>(3),
                    (Gender) reader.GetFieldValue<int>(4),
                    reader.GetFieldValue<DateTime?>(5),
                    reader.GetNullableFieldValue<string>(6),
                    reader.GetFieldValue<string>(7),
                    reader.GetNullableFieldValue<string>(8),
                    reader.GetNullableFieldValue<string>(9),
                    reader.GetNullableFieldValue<string>(10),
                    new Uri(reader.GetFieldValue<string>(11)),
                    reader.GetFieldValue<string[]>(12),
                    reader.GetJsonFieldValue<Dictionary<LinksType, Uri?>>(13) ?? new Dictionary<LinksType, Uri?>(),
                    reader.GetFieldValue<DateTimeOffset>(14),
                    reader.GetFieldValue<DateTimeOffset>(15)
                ),
                cancellationToken);
    }
}