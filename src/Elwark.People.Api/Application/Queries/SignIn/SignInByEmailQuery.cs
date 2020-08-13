using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Shared;
using MediatR;
using Newtonsoft.Json;

namespace Elwark.People.Api.Application.Queries.SignIn
{
    public class SignInByEmailQuery : IRequest<SignInModel>
    {
        public SignInByEmailQuery(Identification.Email email, string password)
        {
            Email = email;
            Password = password;
        }

        public Identification.Email Email { get; }

        public string Password { get; }
    }

    public class SignInByEmailQueryHandler : IRequestHandler<SignInByEmailQuery, SignInModel>
    {
        private readonly IDatabaseQueryExecutor _executor;
        private readonly IPasswordHasher _hasher;

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
           END,
       CASE
           WHEN p.created_at IS NULL THEN NULL
           ELSE json_build_object(
                   '{nameof(SignInDbDataPassword.PasswordModel.Hash)}', encode(p.hash, 'base64'),
                   '{nameof(SignInDbDataPassword.PasswordModel.Salt)}', encode(p.salt, 'base64')
               )
           END
FROM identities i
         LEFT JOIN bans b ON i.account_id = b.account_id
         LEFT JOIN passwords p ON i.account_id = p.account_id
WHERE i.identification_type = @type
  AND i.value = @value;
";

        public SignInByEmailQueryHandler(IDatabaseQueryExecutor executor, IPasswordHasher hasher)
        {
            _executor = executor;
            _hasher = hasher;
        }

        public async Task<SignInModel> Handle(SignInByEmailQuery request, CancellationToken cancellationToken)
        {
            var db = await _executor.SingleAsync(_sql,
                new Dictionary<string, object>
                {
                    {"@type", (int) request.Email.Type},
                    {"@value", request.Email.Value}
                },
                reader =>
                {
                    var banJson = reader.GetNullableFieldValue<string>(3);
                    var passwordJson = reader.GetNullableFieldValue<string>(4);

                    return new SignInDbDataPassword(
                        request.Email,
                        new IdentityId(reader.GetFieldValue<Guid>(0)),
                        new AccountId(reader.GetFieldValue<long>(1)),
                        reader.GetFieldValue<bool>(2),
                        banJson is null
                            ? null
                            : JsonConvert.DeserializeObject<BanDetail>(banJson),
                        passwordJson is null
                            ? null
                            : JsonConvert.DeserializeObject<SignInDbDataPassword.PasswordModel>(passwordJson)
                    );
                },
                cancellationToken
            ) ?? throw ElwarkIdentificationException.NotFound(request.Email);

            db.Validate(_hasher, request.Password);

            return new SignInModel(db.AccountId, db.IdentityId);
        }
    }
}