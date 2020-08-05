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
    public class GetAccountByIdentifierQuery : IRequest<AccountResponse>
    {
        [DebuggerStepThrough]
        public GetAccountByIdentifierQuery(Identification identification) =>
            Identification = identification ?? throw new ArgumentNullException(nameof(identification));

        public Identification Identification { get; }
    }

    public class GetAccountByIdentifierQueryHandler : IRequestHandler<GetAccountByIdentifierQuery, AccountResponse?>
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
WHERE id = (SELECT account_id FROM identities WHERE identification_type = @type AND value = @value);
";

        private readonly IDatabaseQueryExecutor _executor;

        public GetAccountByIdentifierQueryHandler(IDatabaseQueryExecutor executor) =>
            _executor = executor;


        public Task<AccountResponse?> Handle(GetAccountByIdentifierQuery request,
            CancellationToken cancellationToken) =>
            _executor.SingleAsync(Sql,
                new Dictionary<string, object>
                {
                    {"@type", request.Identification.Type},
                    {"@value", request.Identification.Value}
                },
                reader => new AccountResponse(
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