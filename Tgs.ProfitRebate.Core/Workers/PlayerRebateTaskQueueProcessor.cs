using System.Text.Json;
using Json.More;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Tgs.CashService.Client;
using Tgs.Notification.Client;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Repository.Entities;
using Tgs.Service.PlayerBuckets;

namespace Tgs.ProfitRebate.Core.Workers;

public interface IPlayerRebateTaskQueueProcessor
{
    Task ProcessQueue(CancellationToken cancellationToken);
}

internal class PlayerRebateTaskQueueProcessor : IPlayerRebateTaskQueueProcessor
{
    private static readonly Guid[] brandIds =
        { Guid.Parse("0187e5c8-7680-0000-0001-07e79f4d606c"), Guid.Parse("0187eae3-7823-0000-0001-07e785f2b37a") };

    private readonly IPlayerRebateTaskQueue _taskQueue;
    private readonly ICashServiceClient _cashServiceClient;
    private readonly INotificationClient _notificationClient;
    private readonly IPlayerRebateFactory _playerRebateFactory;
    private readonly ITgsService<PlayerRebate> _playerRebateService;
    private readonly ITgsService<Transaction> _transactionService;
    private readonly ILogger<PlayerAllAgentRebateTaskQueueProcessor> _logger;

    public PlayerRebateTaskQueueProcessor(
        IPlayerRebateTaskQueue taskQueue,
        IPlayerRebateFactory playerRebateFactory,
        ICashServiceClient cashServiceClient,
        INotificationClient notificationClient,
        ITgsService<PlayerRebate> playerRebateService,
        ITgsService<Transaction> transactionService,
        ILogger<PlayerAllAgentRebateTaskQueueProcessor> logger)
    {
        _taskQueue = taskQueue;
        _playerRebateFactory = playerRebateFactory;
        _cashServiceClient = cashServiceClient;
        _notificationClient = notificationClient;
        _playerRebateService = playerRebateService;
        _transactionService = transactionService;
        _logger = logger;
    }

    public async Task ProcessQueue(CancellationToken cancellationToken)
    {
        var request = await _taskQueue.DequeueAsync(cancellationToken);

        _logger.LogInformation("player vip rebate request: {request}", request);

        if (await _playerRebateService.GetQueries()
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
                    , cancellationToken)
                .ConfigureAwait(false))
        {
            return;
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

        var rebate = await _playerRebateFactory
            .CreateVipPlayerRebateAsync(vipPlayerRebate, request.IsAutoSend ? DateTimeOffset.UtcNow : null)
            .ConfigureAwait(false);

        if (!request.IsAutoSend)
        {
            return;
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
                    await _playerRebateFactory
                        .CreatePlayerBucketsAsync(new CreatePlayerBucketRequest(
                            request.PlayerCode,
                            BucketType.Vip,
                            playerBucketAmount,
                            rebate.Id.ToString()))
                        .ConfigureAwait(false);
                })
            .ConfigureAwait(false);

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
                    if (!await _transactionService.GetQueries()
                            .AnyAsync(
                                x => x.PlayerCode == request.PlayerCode
                                    && x.ExternalTransactionId == rebate.Id.ToString(),
                                cancellationToken))
                    {
                        _ = await _cashServiceClient.RebateAsync(
                            request.DefaultWalletId,
                            request.RebateAmount,
                            "player rebate",
                            rebate.Id.ToString(), cancellationToken).ConfigureAwait(false);
                    }
                })
            .ConfigureAwait(false);

        await _notificationClient.PushNotificationAsync(request.PlayerCode, "VipRebate", new
                {
                    Amount = request.RebateAmount
                }.ToJsonDocument(new JsonSerializerOptions
                    { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                cancellationToken)
            .ConfigureAwait(false);
    }
}