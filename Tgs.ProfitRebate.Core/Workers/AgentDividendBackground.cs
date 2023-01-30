using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Entities;

namespace Tgs.ProfitRebate.Core.Workers;

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

		await BackgroundProcessing(stoppingToken);
	}

	private async Task BackgroundProcessing(CancellationToken stoppingToken)
	{
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var agentDividendFactory = scope.ServiceProvider.GetRequiredService<IAgentDividendFactory>();
                    var agentDividendService = scope.ServiceProvider.GetRequiredService<ITgsService<AgentDividend>>();

                    var request =
                        await _taskQueue.DequeueAsync(stoppingToken);

                    var now = DateTimeOffset.Now.ToOffset(new(8, 0, 0));
                    var queryDate = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, new(8, 0, 0));

                    await foreach (var brands in
                                   agentDividendFactory.GetSettlementIntervalBrands(request.BrandCode, queryDate)
                                       .WithCancellation(stoppingToken))
                    {
                        var createModels = new List<CreateAgentDividendModel>();
                        var createAutoSendModels = new List<CreateAgentDividendModel>();
                        var updateAgentDividends = new List<AgentDividend>();

                        var maxNumber = await agentDividendService.GetQueries()
                            .MaxAsync(x => (int?)x.Id, request.CancellationToken) ?? 0;

                        foreach (var brand in brands)
                        {
                            if (!await agentDividendService.GetQueries()
                                    .AnyAsync(
                                        x => x.BrandId == brand.Id && brand.CurrentSettlementFrom == x.From &&
                                            brand.CurrentSettlementTo == x.To,
                                        request.CancellationToken))
                            {
                                var (autoSendDividends, manuelOrNegativeDividends, agentDividends, backMaxNumber) =
                                    await agentDividendFactory.GetAgentDividends(brand, maxNumber, stoppingToken);

                                maxNumber = backMaxNumber;

                                createAutoSendModels.AddRange(autoSendDividends);
                                createModels.AddRange(manuelOrNegativeDividends);
                                updateAgentDividends.AddRange(agentDividends);
                            }

                            var (nextFrom, nextTo) = agentDividendFactory.GetInterval(
                                (SettlementIntervalType)brand.SettlementIntervalType,
                                new DateTimeOffset(brand.NextSettlementTo).ToOffset(new(8, 0, 0)),
                                brand.SettlementDay);

                            brand.PreviousSettlementFrom = brand.CurrentSettlementFrom;
                            brand.PreviousSettlementTo = brand.CurrentSettlementTo;
                            brand.CurrentSettlementFrom = brand.NextSettlementFrom;
                            brand.CurrentSettlementTo = brand.NextSettlementTo;
                            brand.NextSettlementFrom = nextFrom.UtcDateTime;
                            brand.NextSettlementTo = nextTo.UtcDateTime;
                        }

                        await agentDividendFactory.SaveAgentDividendData(brands,
                            createModels,
                            createAutoSendModels,
                            updateAgentDividends);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing task work item.");
                }
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
