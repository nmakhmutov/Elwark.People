using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace People.Infrastructure.Confirmations;

internal sealed class ConfirmationsWorker : BackgroundService
{
    private readonly ILogger<ConfirmationsWorker> _logger;
    private readonly IServiceProvider _provider;

    public ConfirmationsWorker(ILogger<ConfirmationsWorker> logger, IServiceProvider provider)
    {
        _logger = logger;
        _provider = provider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await DeleteAsync(stoppingToken);

                _logger.LogDebug("Deleted {c} expired confirmations", result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception occured in {service}", nameof(ConfirmationsWorker));
            }

            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<int> DeleteAsync(CancellationToken ct)
    {
        await using var scope = _provider.CreateAsyncScope();

        var confirmation = scope.ServiceProvider
            .GetRequiredService<IConfirmationService>();

        var result = await confirmation.CleanUpAsync(ct)
            .ConfigureAwait(false);

        return result;
    }
}
