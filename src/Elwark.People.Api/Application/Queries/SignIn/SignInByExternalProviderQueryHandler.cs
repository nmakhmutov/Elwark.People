using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Shared;
using MediatR;
using Newtonsoft.Json;

namespace Elwark.People.Api.Application.Queries.SignIn
{
    public abstract class SignInByExternalProviderQueryHandler<TRequest> : IRequestHandler<TRequest, SignInModel>
        where TRequest : IRequest<SignInModel>
    {
        private readonly IDatabaseQueryExecutor _executor;

        private readonly string _sql = $@"
SELECT i.id,
       i.account_id,
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
WHERE i.identification_type = @type
  AND i.value = @value;
";

        protected SignInByExternalProviderQueryHandler(IDatabaseQueryExecutor executor) =>
            _executor = executor;

        public abstract Task<SignInModel> Handle(TRequest request, CancellationToken cancellationToken);

        protected async Task<SignInDbDataBase<T>> GetDbData<T>(T identifier, CancellationToken cancellationToken)
            where T : Identification =>
            await _executor.SingleAsync(_sql,
                new Dictionary<string, object>
                {
                    {"@type", (int) identifier.Type},
                    {"@value", identifier.Value}
                },
                reader =>
                {
                    var banJson = reader.GetNullableFieldValue<string>(3);

                    return new SignInDbDataBase<T>(
                        identifier,
                        new IdentityId(reader.GetFieldValue<Guid>(0)),
                        new AccountId(reader.GetFieldValue<long>(1)),
                        reader.GetFieldValue<bool>(2),
                        banJson is null
                            ? null
                            : JsonConvert.DeserializeObject<BanDetail>(banJson)
                    );
                },
                cancellationToken
            ) ?? throw ElwarkIdentificationException.NotFound(identifier);
    }
}