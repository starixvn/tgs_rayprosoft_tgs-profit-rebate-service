using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Tgs.CashService.Client;
using Tgs.PlayerService.Client;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Entities;
using Tgs.Service.PlayerBuckets;

namespace Tgs.ProfitRebate.Core.Workers;

public class PlayerAllAgentRebateBackground : BackgroundService
{
    private readonly ILogger<PlayerAllAgentRebateBackground> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPlayerAllAgentRebateTaskQueue _taskQueue;

    public PlayerAllAgentRebateBackground(IPlayerAllAgentRebateTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<PlayerAllAgentRebateBackground> logger)
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
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var playerRebateFactory = scope.ServiceProvider.GetRequiredService<IPlayerRebateFactory>();
                var cashServiceClient = scope.ServiceProvider.GetRequiredService<ICashServiceClient>();
                var playerAllAgentRebateService = scope.ServiceProvider.GetRequiredService<ITgsService<PlayerAllAgentRebate>>();
                var playerServiceClient = scope.ServiceProvider.GetRequiredService<IPlayerServiceClient>();

                var request = await _taskQueue.DequeueAsync(stoppingToken);

                _logger.LogInformation("player all_agent_rebate request: {request}", request);

                var playerAggregate =
                    await playerRebateFactory.GetPlayerAllAgentTurnoverWinLossAsync(request.ReportDate,
                        request.ParentPlayerCode);

                //var activeCount = 0;
                //var turnover = 0M;
                //var winLoss = 0M;

                var playerTurnovers = playerAggregate.GroupBy(x => x.GameType,
                        (gameType, list) => new
                        {
                            GameType = gameType,
                            list
                        })
                    .Select(x => new PlayerAllAgentTurnoverWinLossModel
                    {
                        GameType = x.GameType,
                        PlayerNumber = 
                            x.list.Select(l => l.PlayerCode).Distinct().Count(),
                        SportsTurnover =
                            x.list.Sum(a => a.SportsTurnover),
                        VirtualSportsTurnover =
                            x.list.Sum(a => a.VirtualSportsTurnover),
                        LiveDealerTurnover =
                            x.list.Sum(a => a.LiveDealerTurnover),
                        CardGameTurnover =
                            x.list.Sum(a => a.CardGameTurnover),
                        SlotTurnover =
                            x.list.Sum(a => a.SlotTurnover),
                        LotteryTurnover =
                            x.list.Sum(a => a.LotteryTurnover),
                        SportsWinLoss =
                            x.list.Sum(a => a.SportsWinLoss),
                        VirtualSportsWinLoss =
                            x.list.Sum(a => a.VirtualSportsWinLoss),
                        LiveDealerWinLoss =
                            x.list.Sum(a => a.LiveDealerWinLoss),
                        CardGameWinLoss =
                            x.list.Sum(a => a.CardGameWinLoss),
                        SlotWinLoss =
                            x.list.Sum(a => a.SlotWinLoss),
                        LotteryWinLoss = 
                            x.list.Sum(a => a.LotteryWinLoss)
                    });

                var playerRebates = playerTurnovers
                    .Where(x => MatchConditionSetting(x, request.Conditions))
                    .SelectMany(x => GetRebates(x, request.Conditions, request.ReportDate)).ToArray();

                if (await playerAllAgentRebateService.GetQueries()
                        .AnyAsync(x => x.PlayerCode == request.ParentPlayerCode && x.ReportDate == request.ReportDate.ToUniversalTime(), stoppingToken))
                {
                    _logger.LogInformation("send the all_agent_rebate to player already. request: {request}", request);
                }
                else
                {
                    await playerRebateFactory.CreateAllAgentPlayerRebatesAsync(playerRebates);
                }

                var childPlayers = playerAggregate.Select(x =>
                    {
                        var turnoverCondition = playerRebates.FirstOrDefault(p
                            => p.GameType == x.GameType && p.AllAgentRebateType == AllAgentRebateType.ValidBet);

                        var winLossCondition = playerRebates.FirstOrDefault(p
                            => p.GameType == x.GameType && p.AllAgentRebateType == AllAgentRebateType.WinLoss);

                        return new
                        {
                            PlayerUsername = x.PlayerUsername,
                            PlayerCode = x.PlayerCode,
                            SportsTurnoverRebate = turnoverCondition?.RebatePercentage ?? 0M * x.SportsTurnover,
                            VirtualSportsTurnoverRebate =
                                turnoverCondition?.RebatePercentage ?? 0M * x.VirtualSportsTurnover,
                            LiveDealerTurnoverRebate = turnoverCondition?.RebatePercentage ?? 0M * x.LiveDealerTurnover,
                            CardGameTurnoverRebate = turnoverCondition?.RebatePercentage ?? 0M * x.CardGameTurnover,
                            SlotTurnoverRebate = turnoverCondition?.RebatePercentage ?? 0M * x.SlotTurnover,
                            LotteryTurnoverRebate = turnoverCondition?.RebatePercentage ?? 0M * x.LotteryTurnover,
                            SportsWinLossRebate = winLossCondition?.RebatePercentage ?? 0M * x.SportsWinLoss,
                            VirtualSportsWinLossRebate =
                                winLossCondition?.RebatePercentage ?? 0M * x.VirtualSportsWinLoss,
                            LiveDealerWinLossRebate = winLossCondition?.RebatePercentage ?? 0M * x.LiveDealerWinLoss,
                            CardGameWinLossRebate = winLossCondition?.RebatePercentage ?? 0M * x.CardGameWinLoss,
                            SlotWinLossRebate = winLossCondition?.RebatePercentage ?? 0M * x.SlotWinLoss,
                            LotteryWinLossRebate = winLossCondition?.RebatePercentage ?? 0M * x.LotteryWinLoss,
                        };
                    })
                    .GroupBy(x => new { x.PlayerUsername, x.PlayerCode }, (player, list) => new
                    {
                        PlayerUsername = player.PlayerUsername,
                        PlayerCode = player.PlayerCode,
                        SportsTurnoverRebate = list.Sum(s => s.SportsTurnoverRebate),
                        VirtualSportsTurnoverRebate = list.Sum(s => s.VirtualSportsTurnoverRebate),
                        LiveDealerTurnoverRebate = list.Sum(s => s.LiveDealerTurnoverRebate),
                        CardGameTurnoverRebate = list.Sum(s => s.CardGameTurnoverRebate),
                        SlotTurnoverRebate = list.Sum(s => s.SlotTurnoverRebate),
                        LotteryTurnoverRebate = list.Sum(s => s.LotteryTurnoverRebate),
                        SportsWinLossRebate = list.Sum(s => s.SportsWinLossRebate),
                        VirtualSportsWinLossRebate = list.Sum(s => s.VirtualSportsWinLossRebate),
                        LiveDealerWinLossRebate = list.Sum(s => s.LiveDealerWinLossRebate),
                        CardGameWinLossRebate = list.Sum(s => s.CardGameWinLossRebate),
                        SlotWinLossRebate = list.Sum(s => s.SlotWinLossRebate),
                        LotteryWinLossRebate = list.Sum(s => s.LotteryWinLossRebate),
                        TotalRebate = list.Sum(s => s.SportsTurnoverRebate + s.VirtualSportsTurnoverRebate + s.LiveDealerTurnoverRebate +
                            s.CardGameTurnoverRebate + s.SlotTurnoverRebate + s.LotteryTurnoverRebate + s.SportsWinLossRebate +
                            s.VirtualSportsWinLossRebate + s.LiveDealerWinLossRebate + s.CardGameWinLossRebate + s.SlotWinLossRebate +
                            s.LotteryWinLossRebate)
                    });

                var player = await playerServiceClient.GetPlayerPropertiesAsync(request.ParentPlayerCode, stoppingToken).ConfigureAwait(false);
                var defaultWalletId = player["DefaultWalletId"];

                await playerRebateFactory
                    .CreatePlayerBucketsAsync(childPlayers
                        .Select(x => new CreatePlayerBucketRequest(
                            x.PlayerCode,
                            BucketType.AllAgent,
                            x.TotalRebate,
                            null))
                        .ToArray())
                    .ConfigureAwait(false);

                foreach (var playerBonus in childPlayers)
                {
                    var response = await cashServiceClient.DividendAllAgentRebateAsync(
                        defaultWalletId,
                        playerBonus.RebateAmount,
                        null,
                        null).ConfigureAwait(false);
                }

                await playerRebateFactory.CreateAllAgentPlayerRebateAsync(new PlayerAllAgentRebateModel
                {
                    BrandId = request.BrandId,
                    PlayerCode = request.PlayerCode,
                    ReportDate = request.ReportDate,
                    AllAgentGameType = request.AllAgentGameType,
                    GameType = request.GameType,
                    AllAgentRebateType = request.AllAgentRebateType,
                    Rank = request.Rank,
                    ActiveNumber = request.ActiveNumber,
                    ConditionAmount = request.ConditionAmount,
                    RebatePercentage = request.RebatePercentage,
                    ActualPlayerNumber = request.ActualPlayerNumber,
                    ActualConditionAmount = request.ActualConditionAmount,
                    RebateAmount = request.RebateAmount
                });
                //if (await playerRebateService.GetQueries()
                //        .AnyAsync(x =>
                //            x.BrandId == request.BrandId &&
                //            x.PlayerCode == request.PlayerCode &&
                //            x.ReportDate == request.ReportDate.ToUniversalTime() &&
                //            x.VipCardGameRebatePercentage == request.VipCardGameRebatePercentage &&
                //            x.VipLiveDealerRebatePercentage == request.VipLiveDealerRebatePercentage &&
                //            x.VipLotteryRebatePercentage == request.VipLotteryRebatePercentage &&
                //            x.VipSlotRebatePercentage == request.VipSlotRebatePercentage &&
                //            x.VipSportsRebatePercentage == request.VipSportsRebatePercentage &&
                //            x.VipVirtualSportsRebatePercentage == request.VipVirtualSportsRebatePercentage
                //            , stoppingToken))
                //{
                //    continue;
                //}

                //var vipPlayerRebate = new VipPlayerRebateModel
                //{
                //    BrandId = request.BrandId,
                //    PlayerCode = request.PlayerCode,
                //    AgentId = request.AgentId,
                //    ReportDate = request.ReportDate,
                //    VipCardGameRebatePercentage = request.VipCardGameRebatePercentage,
                //    VipLiveDealerRebatePercentage = request.VipLiveDealerRebatePercentage,
                //    VipLotteryRebatePercentage = request.VipLotteryRebatePercentage,
                //    VipSlotRebatePercentage = request.VipSlotRebatePercentage,
                //    VipSportsRebatePercentage = request.VipSportsRebatePercentage,
                //    VipVirtualSportsRebatePercentage = request.VipVirtualSportsRebatePercentage,
                //    SportsTurnover = request.SportsTurnover,
                //    SportsRebateAmount = request.SportsRebateAmount,
                //    VirtualSportsTurnover = request.VirtualSportsTurnover,
                //    VirtualSportsRebateAmount = request.VirtualSportsRebateAmount,
                //    LiveDealerTurnover = request.LiveDealerTurnover,
                //    LiveDealerRebateAmount = request.LiveDealerRebateAmount,
                //    CardGameTurnover = request.CardGameTurnover,
                //    CardGameRebateAmount = request.CardGameRebateAmount,
                //    SlotTurnover = request.SlotTurnover,
                //    SlotRebateAmount = request.SlotRebateAmount,
                //    LotteryTurnover = request.LotteryTurnover,
                //    LotteryRebateAmount = request.LotteryRebateAmount,
                //    RebateAmount = request.RebateAmount
                //};

                //var rebate = await playerRebateFactory
                //    .CreateVipPlayerRebateAsync(vipPlayerRebate, request.IsAutoSend ? DateTimeOffset.UtcNow : null)
                //    .ConfigureAwait(false);

                //if (!request.IsAutoSend)
                //{
                //    continue;
                //}

                //_logger.LogInformation("player vip rebate create player bucket request: {request.PlayerCode} {request.RebateAmount}", request.PlayerCode, request.RebateAmount);

                //await Policy.Handle<Exception>()
                //    .RetryAsync(
                //        1,
                //        (exception, retryCount) => _logger.LogError(
                //            "PlayerCode:{request.PlayerCode} RebateAmount:{request.RebateAmount} retryCount:{retryCount} Exception:{exception}",
                //            request.PlayerCode,
                //            request.RebateAmount,
                //            retryCount,
                //            exception))
                //    .ExecuteAsync(
                //        async () =>
                //        {
                //            await playerRebateFactory
                //                .CreatePlayerBucketsAsync(new CreatePlayerBucketRequest(
                //                    request.PlayerCode,
                //                    Service.PlayerBuckets.BucketType.Vip,
                //                    request.RebateAmount,
                //                    rebate.Id.ToString()))
                //                .ConfigureAwait(false);
                //        });

                //_logger.LogInformation("player vip rebate send request: {request.PlayerCode} {request.RebateAmount}", request.PlayerCode, request.RebateAmount);

                //await Policy.Handle<HttpRequestException>()
                //    .WaitAndRetryAsync(
                //        1,
                //        retryAttempt => TimeSpan.FromSeconds(3),
                //        (exception, time, retryCount, context) => _logger.LogError(
                //            "externalId:{externalId} retryCount:{retryCount} Exception:{exception}",
                //            rebate.Id,
                //            retryCount,
                //            exception))
                //    .ExecuteAsync(
                //        async () =>
                //        {
                //            if (!await transactionService.GetQueries()
                //                    .AnyAsync(
                //                        x => x.PlayerCode == request.PlayerCode
                //                            && x.ExternalTransactionId == rebate.Id.ToString(),
                //                        cancellationToken: stoppingToken))
                //            {
                //                _ = await cashServiceClient.RebateAsync(
                //                    request.DefaultWalletId,
                //                    request.RebateAmount,
                //                    "player rebate",
                //                    rebate.Id.ToString(), stoppingToken).ConfigureAwait(false);
                //            }
                //        });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing task work item.");
            }
    }

    private bool MatchConditionSetting(PlayerAllAgentTurnoverWinLossModel model,
        IEnumerable<BrandAllAgentGameTypeConditionModel> conditions)
    {
        return conditions.Any(x => model.BrandId == x.BrandId &&
            ((short)model.GameType == (short)x.AllAgentGameType ||
                model.GameType == GameType.Sportsbook && Consts.SportsGroup.Contains(model.GameType) ||
                model.GameType == GameType.Slot && Consts.SlotGroup.Contains(model.GameType)));
    }

    private IEnumerable<PlayerAllAgentRebateModel> GetRebates(PlayerAllAgentTurnoverWinLossModel model,
        IEnumerable<BrandAllAgentGameTypeConditionModel> gameTypeConditions, DateTimeOffset reportDate)
    {
        var conditions = gameTypeConditions.Where(x => model.BrandId == x.BrandId &&
            ((short)model.GameType == (short)x.AllAgentGameType ||
                model.GameType == GameType.Sportsbook && Consts.SportsGroup.Contains(model.GameType) ||
                model.GameType == GameType.Slot && Consts.SlotGroup.Contains(model.GameType))).ToArray();

        var validBetConditions = conditions.Where(x
            => x.HasValidBet && (short)x.AllAgentRebateType == (short)AllAgentRebateType.ValidBet).ToArray();

        var winLossConditions = conditions.Where(x
            => x.HasWinLoss && (short)x.AllAgentRebateType == (short)AllAgentRebateType.WinLoss).ToArray();

        var validBetRebateCondition = new BrandAllAgentGameTypeConditionModel();
        var winLossRebateCondition = new BrandAllAgentGameTypeConditionModel();
        var allAgentGameType = AllAgentGameType.Sportsbook;
        var actualTurnoverAmount = 0M;
        var actualWinLossAmount = 0M;

        switch (model.GameType)
        {
            case GameType.Sportsbook or GameType.ESports:
                allAgentGameType = AllAgentGameType.Sportsbook;
                validBetRebateCondition = validBetConditions.Where(x => x.AllAgentGameType == allAgentGameType)
                    .OrderByDescending(x => x.Rank).FirstOrDefault(x => model.PlayerNumber >= x.ActiveNumber &&
                        model.SportsTurnover >= x.NetProfit);
                winLossRebateCondition = winLossConditions.Where(x => x.AllAgentGameType == allAgentGameType)
                    .OrderByDescending(x => x.Rank).FirstOrDefault(x => model.PlayerNumber >= x.ActiveNumber &&
                        model.SportsWinLoss >= x.NetProfit);
                actualTurnoverAmount = model.SportsTurnover;
                actualWinLossAmount = model.SportsWinLoss;
                break;

            case GameType.VirtualSports:
                allAgentGameType = AllAgentGameType.VirtualSports;
                validBetRebateCondition = validBetConditions.Where(x => x.AllAgentGameType == allAgentGameType)
                    .OrderByDescending(x => x.Rank).FirstOrDefault(x => model.PlayerNumber >= x.ActiveNumber &&
                        model.VirtualSportsTurnover >= x.NetProfit);
                winLossRebateCondition = winLossConditions.Where(x => x.AllAgentGameType == allAgentGameType)
                    .OrderByDescending(x => x.Rank).FirstOrDefault(x => model.PlayerNumber >= x.ActiveNumber &&
                        model.VirtualSportsWinLoss >= x.NetProfit);
                actualTurnoverAmount = model.VirtualSportsTurnover;
                actualWinLossAmount = model.VirtualSportsWinLoss;
                break;

            case GameType.LiveDealer:
                allAgentGameType = AllAgentGameType.LiveDealer;
                validBetRebateCondition = validBetConditions.Where(x => x.AllAgentGameType == allAgentGameType)
                    .OrderByDescending(x => x.Rank).FirstOrDefault(x => model.PlayerNumber >= x.ActiveNumber &&
                        model.LiveDealerTurnover >= x.NetProfit);
                winLossRebateCondition = winLossConditions.Where(x => x.AllAgentGameType == allAgentGameType)
                    .OrderByDescending(x => x.Rank).FirstOrDefault(x => model.PlayerNumber >= x.ActiveNumber &&
                        model.LiveDealerWinLoss >= x.NetProfit);
                actualTurnoverAmount = model.LiveDealerTurnover;
                actualWinLossAmount = model.LiveDealerWinLoss;
                break;

            case GameType.CardGame:
                allAgentGameType = AllAgentGameType.CardGame;
                validBetRebateCondition = validBetConditions.Where(x => x.AllAgentGameType == allAgentGameType)
                    .OrderByDescending(x => x.Rank).FirstOrDefault(x => model.PlayerNumber >= x.ActiveNumber &&
                        model.CardGameTurnover >= x.NetProfit);
                winLossRebateCondition = winLossConditions.Where(x => x.AllAgentGameType == allAgentGameType)
                    .OrderByDescending(x => x.Rank).FirstOrDefault(x => model.PlayerNumber >= x.ActiveNumber &&
                        model.CardGameWinLoss >= x.NetProfit);
                actualTurnoverAmount = model.CardGameTurnover;
                actualWinLossAmount = model.CardGameWinLoss;
                break;

            case GameType.Fishing or GameType.Arcade or GameType.Slot or GameType.TableGame:
                allAgentGameType = AllAgentGameType.Slot;
                validBetRebateCondition = validBetConditions.Where(x => x.AllAgentGameType == allAgentGameType)
                    .OrderByDescending(x => x.Rank).FirstOrDefault(x => model.PlayerNumber >= x.ActiveNumber &&
                        model.SlotTurnover >= x.NetProfit);
                winLossRebateCondition = winLossConditions.Where(x => x.AllAgentGameType == allAgentGameType)
                    .OrderByDescending(x => x.Rank).FirstOrDefault(x => model.PlayerNumber >= x.ActiveNumber &&
                        model.SlotWinLoss >= x.NetProfit);
                actualTurnoverAmount = model.SlotTurnover;
                actualWinLossAmount = model.SlotWinLoss;
                break;

            case GameType.Lottery:
                allAgentGameType = AllAgentGameType.Lottery;
                validBetRebateCondition = validBetConditions.Where(x => x.AllAgentGameType == allAgentGameType)
                    .OrderByDescending(x => x.Rank).FirstOrDefault(x => model.PlayerNumber >= x.ActiveNumber &&
                        model.LotteryTurnover >= x.NetProfit);
                winLossRebateCondition = winLossConditions.Where(x => x.AllAgentGameType == allAgentGameType)
                    .OrderByDescending(x => x.Rank).FirstOrDefault(x => model.PlayerNumber >= x.ActiveNumber &&
                        model.LotteryWinLoss >= x.NetProfit);
                actualTurnoverAmount = model.LotteryTurnover;
                actualWinLossAmount = model.LotteryWinLoss;
                break;
        }

        return new[]
        {
            new PlayerAllAgentRebateModel
            {
                BrandId = model.BrandId,
                PlayerCode = model.ParentPlayerCode,
                AllAgentGameType = allAgentGameType,
                ReportDate = reportDate.ToUniversalTime(),
                GameType = model.GameType,
                AllAgentRebateType = AllAgentRebateType.ValidBet,
                Rank = validBetRebateCondition?.Rank ?? 0,
                ActiveNumber = validBetRebateCondition?.ActiveNumber ?? 0,
                ConditionAmount = validBetRebateCondition?.NetProfit ?? 0M,
                RebatePercentage = validBetRebateCondition?.RebatePercentage ?? 0M,
                ActualPlayerNumber = model.PlayerNumber,
                ActualConditionAmount = actualTurnoverAmount,
                RebateAmount = actualTurnoverAmount * (validBetRebateCondition?.RebatePercentage ?? 0M) / 100
            },
            new PlayerAllAgentRebateModel
            {
                BrandId = model.BrandId,
                PlayerCode = model.ParentPlayerCode,
                AllAgentGameType = allAgentGameType,
                ReportDate = reportDate.ToUniversalTime(),
                GameType = model.GameType,
                AllAgentRebateType = AllAgentRebateType.WinLoss,
                Rank = winLossRebateCondition?.Rank ?? 0,
                ActiveNumber = winLossRebateCondition?.ActiveNumber ?? 0,
                ConditionAmount = winLossRebateCondition?.NetProfit ?? 0M,
                RebatePercentage = winLossRebateCondition?.RebatePercentage ?? 0M,
                ActualPlayerNumber = model.PlayerNumber,
                ActualConditionAmount = actualWinLossAmount,
                RebateAmount = actualWinLossAmount * (winLossRebateCondition?.RebatePercentage ?? 0M) / 100
            }
        };
    }

}