using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Shared;
using MediatR;
using Newtonsoft.Json;

namespace Elwark.People.Api.Application.Queries
{
    public class CheckIdentityQuery : IRequest<IdentityActiveResponse>
    {
        public CheckIdentityQuery(IdentityId identityId) =>
            IdentityId = identityId;

        public IdentityId IdentityId { get; }
    }

    public class CheckIdentityQueryHandler : IRequestHandler<CheckIdentityQuery, IdentityActiveResponse>
    {
        private readonly IDatabaseQueryExecutor _executor;

        private readonly string _sql = $@"
SELECT i.id,
       CASE WHEN i.confirmed_at IS NULL THEN FALSE ELSE TRUE END,
       CASE
           WHEN b.type IS NULL THEN NULL
           ELSE json_build_object(
                   '{nameof(BanDetail.Type)}', b.type,
                   '{nameof(BanDetail.CreatedAt)}', b.created_at,
                   '{nameof(BanDetail.ExpiredAt)}', b.expired_at,
                   '{nameof(BanDetail.Reason)}', b.reason
               )
           END
FROM identities i
         LEFT JOIN bans b ON i.account_id = b.account_id
WHERE i.id = @id;
";

        public CheckIdentityQueryHandler(IDatabaseQueryExecutor executor) =>
            _executor = executor;

        public async Task<IdentityActiveResponse> Handle(CheckIdentityQuery request,
            CancellationToken cancellationToken)
        {
            var data = await _executor.SingleAsync(_sql,
                new Dictionary<string, object>
                {
                    {"@id", request.IdentityId.Value}
                },
                reader =>
                {
                    var banJson = reader.GetNullableFieldValue<string>(2);
                    return new Result(
                        new IdentityId(reader.GetFieldValue<Guid>(0)),
                        reader.GetFieldValue<bool>(1),
                        banJson is null
                            ? null
                            : JsonConvert.DeserializeObject<BanDetail>(banJson)
                        );
                },
                cancellationToken);

            if (data is null)
                return IdentityActiveResponse.Deactivated;

            if (!data.IsConfirmed)
                return IdentityActiveResponse.Deactivated;

            if (data.Ban is {})
                return IdentityActiveResponse.Deactivated;

            return IdentityActiveResponse.Activated;
        }
        
        private class Result
        {
            public Result(IdentityId id, bool isConfirmed, BanDetail? ban)
            {
                Id = id;
                IsConfirmed = isConfirmed;
                Ban = ban;
            }

            public IdentityId Id { get; }
            
            public bool IsConfirmed { get; }
            
            public BanDetail? Ban { get; }
        }
    }
}