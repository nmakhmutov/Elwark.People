using MediatR;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain;
using People.Domain.Entities;
using People.Infrastructure.Providers.NpgsqlData;
using People.Kafka.Integration;

namespace People.Api.Application.Queries.IsAccountActive;

internal sealed record IsAccountActiveQuery(AccountId Id) : IRequest<bool>;

internal sealed class IsAccountActiveQueryHandler : IRequestHandler<IsAccountActiveQuery, bool>
{
    private readonly IIntegrationEventBus _bus;
    private readonly INpgsqlDataProvider _dataProvider;
    private readonly TimeProvider _timeProvider;

    public IsAccountActiveQueryHandler(IIntegrationEventBus bus, INpgsqlDataProvider dataProvider,
        TimeProvider timeProvider)
    {
        _bus = bus;
        _dataProvider = dataProvider;
        _timeProvider = timeProvider;
    }

    public async Task<bool> Handle(IsAccountActiveQuery request, CancellationToken ct)
    {
        var data = await _dataProvider
            .Sql($"SELECT is_activated, ban IS NOT NULL FROM accounts WHERE id = {request.Id} LIMIT 1")
            .Select(x => new { IsActivated = x.GetBoolean(0), IsBanned = x.GetBoolean(1) })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (data is null)
            return false;

        var evt = new AccountEngaged.CheckedActivityIntegrationEvent(
            Guid.NewGuid(),
            _timeProvider.UtcNow(),
            request.Id
        );

        await _bus.PublishAsync(evt, ct)
            .ConfigureAwait(false);

        return data is { IsActivated: true, IsBanned: false };
    }
}
