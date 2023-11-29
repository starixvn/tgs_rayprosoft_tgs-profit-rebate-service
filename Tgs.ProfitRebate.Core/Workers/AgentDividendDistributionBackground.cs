using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.ProfitRebate.Repository.Entities;

namespace Tgs.ProfitRebate.Core.Workers;

public class AgentDividendDistributionBackground : BackgroundService
{
    private readonly ILogger<AgentDividendDistributionBackground> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAgentDividendDistributionTaskQueue _taskQueue;
    private readonly ICashServiceClient _cashServiceClient;

    public AgentDividendDistributionBackground(IAgentDividendDistributionTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<AgentDividendDistributionBackground> logger,
        ICashServiceClient cashServiceClient)
    {
        _taskQueue = taskQueue;
        _logger = logger;
        _cashServiceClient = cashServiceClient;
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
            "Queued Hosted Service is running.{0}" +
            "{1}Tap W to add a work item to the " +
            "background queue.{2}",
            Environment.NewLine,
            Environment.NewLine,
            Environment.NewLine);

        await BackgroundProcessingAsync(stoppingToken);
    }

    private async Task BackgroundProcessingAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var agentService = scope.ServiceProvider.GetRequiredService<ITgsService<Agent>>();
                    var agentDividendService = scope.ServiceProvider.GetRequiredService<ITgsService<AgentDividend>>();

                    var request = await _taskQueue.DequeueAsync(stoppingToken);

                    var agent = await agentService.GetQueries().FirstAsync(x => x.Id == request.AgentId, stoppingToken);

                    await agentDividendService.GetQueries()
                        .Where(a => a.Id == request.ExternalId)
                        .ExecuteUpdateAsync(x => x.SetProperty(s => s.SentOn, DateTime.UtcNow), stoppingToken);

                    await _cashServiceClient.AgentDividendAsync(agent.WalletId!.Value,
                        request.DividendAmount,
                        null,
                        request.ExternalId.ToString(),
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing task work item.");
                }

            _logger.LogInformation(
                "Execution ended. Cancellation token cancelled = {IsCancellationRequested}",
                stoppingToken.IsCancellationRequested);
        }
        catch (Exception ex) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Execution Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception. Execution Stopping");
        }
    }
}