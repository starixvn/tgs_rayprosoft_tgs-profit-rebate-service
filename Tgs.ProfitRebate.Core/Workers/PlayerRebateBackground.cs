using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Tgs.CashService.Client;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Entities;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Core.Workers;

public class PlayerRebateBackground : BackgroundService
{
    private readonly ILogger<PlayerRebateBackground> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPlayerRebateTaskQueue _taskQueue;

    public PlayerRebateBackground(IPlayerRebateTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<PlayerRebateBackground> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Player Rebate Queued Hosted Service is running.");

        await BackgroundProcessing(stoppingToken);
    }

    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var playerRebateFactory = scope.ServiceProvider.GetRequiredService<IPlayerRebateFactory>();
                var cashServiceClient = scope.ServiceProvider.GetRequiredService<ICashServiceClient>();
                var transactionService = scope.ServiceProvider.GetRequiredService<ITgsService<Transaction>>();

                var request =
                    await _taskQueue.DequeueAsync(stoppingToken);

                var vipPlayerRebate = new VipPlayerRebateModel
                {
                    BrandId = request.BrandId,
                    PlayerCode = request.PlayerCode,
                    AgentId = request.AgentId,
                    ReportDate = request.ReportDate,
                    VipCardGameRebatePercentage = request.VipCardGameRebatePercentage,
                    VipLiveDealerRebatePercentage = request.VipLiveDealerRebatePercentage,
                    VipLotteryRebatePercentage = request.VipLotteryRebatePercentage,
                    VipSlotRebatePercentage = request.VipSlotRebatePercentage,
                    VipSportsRebatePercentage = request.VipSportsRebatePercentage,
                    VipVirtualSportsRebatePercentage = request.VipVirtualSportsRebatePercentage,
                    SportsTurnover = request.SportsTurnover,
                    SportsRebateAmount = request.SportsRebateAmount,
                    VirtualSportsTurnover = request.VirtualSportsTurnover,
                    VirtualSportsRebateAmount = request.VirtualSportsRebateAmount,
                    LiveDealerTurnover = request.LiveDealerTurnover,
                    LiveDealerRebateAmount = request.LiveDealerRebateAmount,
                    CardGameTurnover = request.CardGameTurnover,
                    CardGameRebateAmount = request.CardGameRebateAmount,
                    SlotTurnover = request.SlotTurnover,
                    SlotRebateAmount = request.SlotRebateAmount,
                    LotteryTurnover = request.LotteryTurnover,
                    LotteryRebateAmount = request.LotteryRebateAmount,
                    RebateAmount = request.RebateAmount
                };

                var rebate = await playerRebateFactory
                    .CreateVipPlayerRebateAsync(vipPlayerRebate, request.IsAutoSend ? DateTimeOffset.UtcNow : null)
                    .ConfigureAwait(false);

                if (!request.IsAutoSend)
                {
                    continue;
                }

                await playerRebateFactory
                    .CreatePlayerBucketAsync(new(request.PlayerCode, BucketType.Vip, request.RebateAmount))
                    .ConfigureAwait(false);

                await Policy.Handle<HttpRequestException>()
                    .WaitAndRetryAsync(1, retryAttempt =>
                        TimeSpan.FromSeconds(3), (exception, time, retryCount, context) =>
                    {
                        _logger.LogError("externalId:{externalId} retryCount:{retryCount} Exception:{exception}",
                            rebate.Id, retryCount, exception);
                    })
                    .ExecuteAsync(
                        async () =>
                        {
                            if (!await transactionService.GetQueries()
                                    .AnyAsync(
                                        x => x.PlayerCode == request.PlayerCode &&
                                            x.ExternalTransactionId == rebate.Id.ToString(),
                                        cancellationToken: stoppingToken))
                            {
                                await cashServiceClient.RebateAsync(
                                    request.DefaultWalletId,
                                    request.RebateAmount,
                                    "player rebate",
                                    rebate.Id.ToString()).ConfigureAwait(false);
                            }
                        });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing task work item.");
            }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queued Hosted Service is stopping.");

        await base.StopAsync(stoppingToken);
    }
}