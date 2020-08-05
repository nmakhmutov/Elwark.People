using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Background.Background
{
    public abstract class IntervalBackgroundService<T> : BackgroundService
    {
        protected readonly ILogger<T> Logger;

        protected IntervalBackgroundService(ILogger<T> logger) =>
            Logger = logger;

        protected abstract TimeSpan Delay { get; }

        protected abstract Task ExecuteTaskAsync(CancellationToken cancellationToken);

        protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Starting background service: '{0}' with interval '{1}'", typeof(T).Name, Delay);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExecuteTaskAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Unhandled exception in background service '{0}'", typeof(T).Name);
                }

                await Task.Delay(Delay, stoppingToken);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Stopping background service: '{0}'", typeof(T).Name);

            return base.StopAsync(cancellationToken);
        }
    }
}