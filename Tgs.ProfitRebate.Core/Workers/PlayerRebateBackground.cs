using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Tgs.CashService.Client;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Repository.Entities;
using Tgs.Service.PlayerBuckets;

namespace Tgs.ProfitRebate.Core.Workers;

public class PlayerRebateBackground : BackgroundService
{
    private static readonly Guid[] brandIds =
        { Guid.Parse("0187e5c8-7680-0000-0001-07e79f4d606c"), Guid.Parse("0187eae3-7823-0000-0001-07e785f2b37a") };

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

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queued Hosted Service is stopping.");

        await base.StopAsync(stoppingToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Player Rebate Queued Hosted Service is running.");

        await BackgroundProcessing(stoppingToken);
    }

    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var playerRebateFactory = scope.ServiceProvider.GetRequiredService<IPlayerRebateFactory>();
                    var cashServiceClient = scope.ServiceProvider.GetRequiredService<ICashServiceClient>();
                    var transactionService = scope.ServiceProvider.GetRequiredService<ITgsService<Transaction>>();
                    var playerRebateService = scope.ServiceProvider.GetRequiredService<ITgsService<PlayerRebate>>();

                    var request = await _taskQueue.DequeueAsync(stoppingToken);

                    _logger.LogInformation("player vip rebate request: {request}", request);

                    if (await playerRebateService.GetQueries()
                            .AnyAsync(x =>
                                    x.BrandId == request.BrandId &&
                                    x.PlayerCode == request.PlayerCode &&
                                    x.ReportDate == request.ReportDate.ToUniversalTime() &&
                                    x.VipCardGameRebatePercentage == request.VipCardGameRebatePercentage &&
                                    x.VipLiveDealerRebatePercentage == request.VipLiveDealerRebatePercentage &&
                                    x.VipLotteryRebatePercentage == request.VipLotteryRebatePercentage &&
                                    x.VipSlotRebatePercentage == request.VipSlotRebatePercentage &&
                                    x.VipSportsRebatePercentage == request.VipSportsRebatePercentage &&
                                    x.VipVirtualSportsRebatePercentage == request.VipVirtualSportsRebatePercentage
                                , stoppingToken))
                    {
                        continue;
                    }

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

                    var playerBucketAmount = brandIds.Contains(request.BrandId)
                        ? request.RebateAmount * 5
                        : request.RebateAmount;

                    _logger.LogInformation(
                        "player vip rebate create player bucket request: {request.PlayerCode} {request.RebateAmount},bucket amount {playerBucketAmount}",
                        request.PlayerCode, request.RebateAmount, playerBucketAmount);

                    await Policy.Handle<Exception>()
                        .RetryAsync(
                            3,
                            (exception, retryCount) => _logger.LogError(
                                "PlayerCode:{request.PlayerCode} BucketAmount:{playerBucketAmount} retryCount:{retryCount} Exception:{exception}",
                                request.PlayerCode,
                                playerBucketAmount,
                                retryCount,
                                exception))
                        .ExecuteAsync(
                            async () =>
                            {
                                await playerRebateFactory
                                    .CreatePlayerBucketsAsync(new CreatePlayerBucketRequest(
                                        request.PlayerCode,
                                        BucketType.Vip,
                                        playerBucketAmount,
                                        rebate.Id.ToString()))
                                    .ConfigureAwait(false);
                            });

                    _logger.LogInformation(
                        "player vip rebate send request: {request.PlayerCode} {request.RebateAmount}",
                        request.PlayerCode, request.RebateAmount);

                    await Policy.Handle<HttpRequestException>()
                        .WaitAndRetryAsync(
                            3,
                            retryAttempt => TimeSpan.FromSeconds(3),
                            (exception, time, retryCount, context) => _logger.LogError(
                                "externalId:{externalId} retryCount:{retryCount} Exception:{exception}",
                                rebate.Id,
                                retryCount,
                                exception))
                        .ExecuteAsync(
                            async () =>
                            {
                                if (!await transactionService.GetQueries()
                                        .AnyAsync(
                                            x => x.PlayerCode == request.PlayerCode
                                                && x.ExternalTransactionId == rebate.Id.ToString(),
                                            stoppingToken))
                                {
                                    _ = await cashServiceClient.RebateAsync(
                                        request.DefaultWalletId,
                                        request.RebateAmount,
                                        "player rebate",
                                        rebate.Id.ToString(), stoppingToken).ConfigureAwait(false);
                                }
                            });
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