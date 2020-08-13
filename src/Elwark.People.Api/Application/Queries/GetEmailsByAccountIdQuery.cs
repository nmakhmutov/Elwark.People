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
    public class GetEmailsByAccountIdQuery : IRequest<IReadOnlyCollection<EmailModel>>
    {
        [DebuggerStepThrough]
        public GetEmailsByAccountIdQuery(AccountId id) =>
            Id = id;

        public AccountId Id { get; }
    }

    public class GetEmailsByAccountIdQueryHandler
        : IRequestHandler<GetEmailsByAccountIdQuery, IReadOnlyCollection<EmailModel>>
    {
        private const string Sql = @"
SELECT value,
       notification_type,
       CASE
           WHEN confirmed_at IS NULL THEN FALSE
           ELSE TRUE
           END
FROM identities
WHERE account_id = @id
  AND identification_type = @type
";

        private readonly IDatabaseQueryExecutor _executor;

        public GetEmailsByAccountIdQueryHandler(IDatabaseQueryExecutor executor) =>
            _executor = executor;

        public Task<IReadOnlyCollection<EmailModel>> Handle(GetEmailsByAccountIdQuery request,
            CancellationToken cancellationToken)
        {
            return _executor.MultiplyAsync(Sql,
                new Dictionary<string, object>
                {
                    {"@id", request.Id.Value},
                    {"@type", (int) IdentificationType.Email}
                },
                reader => new EmailModel(
                    Notification.Create(reader.GetFieldValue<int>(1), reader.GetFieldValue<string>(0)),
                    reader.GetFieldValue<bool>(2)
                ),
                cancellationToken);
        }
    }
}