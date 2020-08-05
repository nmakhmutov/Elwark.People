using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Shared;
using MediatR;

namespace Elwark.People.Api.Application.Queries
{
    public class GetNotifierQuery : IRequest<Notification?>
    {
        [DebuggerStepThrough]
        public GetNotifierQuery(AccountId accountId, NotificationType type)
        {
            AccountId = accountId;
            Type = type;
        }

        public AccountId AccountId { get; }

        public NotificationType Type { get; }
    }

    public class GetNotifierQueryHandler : IRequestHandler<GetNotifierQuery, Notification?>
    {
        private const string Sql = @"
SELECT i.value
FROM identities i
WHERE i.notification_type = @type
  AND i.account_id = @id;
";

        private readonly IDatabaseQueryExecutor _executor;

        public GetNotifierQueryHandler(IDatabaseQueryExecutor executor) =>
            _executor = executor;

        public Task<Notification?> Handle(GetNotifierQuery request, CancellationToken cancellationToken) =>
            _executor.SingleAsync(Sql,
                new Dictionary<string, object>
                {
                    {"@id", request.AccountId.Value},
                    {"@type", (int) request.Type}
                },
                reader => Notification.Create(request.Type, reader.GetFieldValue<string>(0)),
                cancellationToken
            );
    }
}