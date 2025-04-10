using EtlService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EtlService.Infrastructure.BackgroundJobs
{
    public class BackfillJob : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<BackfillJob> _logger;
        public BackfillJob(IServiceProvider provider, ILogger<BackfillJob> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _provider.CreateScope();
                var backfillService = scope.ServiceProvider.GetRequiredService<BackfillService>();

                try
                {
                    _logger.LogInformation("Backfill started for {Symbol}", "AAPL");
                    await backfillService.BackfillLast30DaysAsync("AAPL", "15min");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Backfill failed: {Message}", ex.Message);
                }

                // Wait for 24 hours before the next run
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("Backfill completed for {Symbol}", "AAPL");
        }
    }
}
