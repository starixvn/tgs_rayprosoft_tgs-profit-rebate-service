using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tgs.ProfitRebate.Core.Workers
{
    public class AgentDividendBackground : BackgroundService
    {
        private readonly ILogger<AgentDividendBackground> _logger;
        private readonly IServiceProvider _serviceProvider;

        private readonly IAgentDividendTaskQueue _taskQueue;

        public AgentDividendBackground(IAgentDividendTaskQueue taskQueue,
            IServiceProvider serviceProvider,
            ILogger<AgentDividendBackground> logger)
        {
            _taskQueue = taskQueue;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"Queued Hosted Service is running.{Environment.NewLine}" +
                $"{Environment.NewLine}Tap W to add a work item to the " +
                $"background queue.{Environment.NewLine}");

            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var workItem =
                        await _taskQueue.DequeueAsync(stoppingToken);

                    await workItem.func(workItem.model);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing task work item.");
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued Hosted Service is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}