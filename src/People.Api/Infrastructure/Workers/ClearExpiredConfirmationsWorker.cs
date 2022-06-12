using People.Infrastructure.Providers.NpgsqlData;

namespace People.Api.Infrastructure.Workers;

internal sealed class ClearExpiredConfirmationsWorker : BackgroundService
{
    private readonly INpgsqlDataProvider _provider;
    private readonly ILogger<ClearExpiredConfirmationsWorker> _logger;

    public ClearExpiredConfirmationsWorker(INpgsqlDataProvider provider,
        ILogger<ClearExpiredConfirmationsWorker> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await _provider
                .Sql("DELETE FROM confirmations WHERE expires_at < now()")
                .ExecuteAsync(stoppingToken);
            
            if (result > 0)
                _logger.LogInformation("Cleared {c} expired confirmations", result);
            
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
