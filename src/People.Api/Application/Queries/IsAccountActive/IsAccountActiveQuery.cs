using MediatR;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain.Entities;
using People.Infrastructure.Providers.NpgsqlData;
using People.Kafka.Integration;

namespace People.Api.Application.Queries.IsAccountActive;

internal sealed record IsAccountActiveQuery(AccountId Id) : IRequest<bool>;

internal sealed class IsAccountActiveQueryHandler : IRequestHandler<IsAccountActiveQuery, bool>
{
    private readonly IIntegrationEventBus _bus;
    private readonly INpgsqlDataProvider _dataProvider;

    public IsAccountActiveQueryHandler(IIntegrationEventBus bus, INpgsqlDataProvider dataProvider)
    {
        _bus = bus;
        _dataProvider = dataProvider;
    }

    public async Task<bool> Handle(IsAccountActiveQuery request, CancellationToken ct)
    {
        var data = await _dataProvider
            .Sql($"SELECT is_activated, ban IS NOT NULL FROM accounts WHERE id = {request.Id} LIMIT 1")
            .Select(x => new
            {
                IsActivated = x.GetBoolean(0),
                IsBanned = x.GetBoolean(1)
            })
            .FirstOrDefaultAsync(ct);

        if (data is null)
            return false;

        var evt = new AccountActivity.InspectedIntegrationEvent(request.Id);

        await _bus.PublishAsync(evt, ct);

        return data is { IsActivated: true, IsBanned: false };
    }
}
