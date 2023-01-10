using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tgs.ProfitRebate.Core.Workers;

public class PlayerAllAgentRebateBackground : BackgroundService
{
    private readonly ILogger<PlayerAllAgentRebateBackground> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PlayerAllAgentRebateBackground(
        IServiceProvider serviceProvider,
        ILogger<PlayerAllAgentRebateBackground> logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queued Hosted Service is stopping.");

        await base.StopAsync(stoppingToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Player All Agent Rebate Queued Hosted Service is running.");

        await BackgroundProcessing(stoppingToken);
    }

    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IPlayerAllAgentRebateTaskQueueProcessor>();
                await processor.ProcessQueue(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing task work item.");
            }
    }
}