using Microsoft.Extensions.Logging;
using Polly;
using Microsoft.EntityFrameworkCore;
using Tgs.CashService.Client;
using Tgs.PlayerService.Client;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Entities;
using Tgs.Service.PlayerBuckets;

namespace Tgs.ProfitRebate.Core.Workers;

public interface IPlayerAllAgentRebateTaskQueueProcessor
{
	Task ProcessQueue(CancellationToken cancellationToken);
}

public class PlayerAllAgentRebateTaskQueueProcessor : IPlayerAllAgentRebateTaskQueueProcessor
{
    private readonly IPlayerAllAgentRebateTaskQueue _taskQueue;
    private readonly IPlayerAllAgentRebateFactory _playerRebateFactory;
    private readonly ICashServiceClient _cashServiceClient;
    private readonly IPlayerServiceClient _playerServiceClient;
    private readonly ITgsService<PlayerAllAgentRebateRecord> _playerAllAgentRebateService;
    private readonly ITgsService<PlayerAllAgentRebateContributor> _playerAllAgentRebateContributorService;
    private readonly ITgsService<Transaction> _transactionService;
    private readonly ILogger<PlayerAllAgentRebateTaskQueueProcessor> _logger;

    public PlayerAllAgentRebateTaskQueueProcessor(
        IPlayerAllAgentRebateTaskQueue taskQueue,
        IPlayerAllAgentRebateFactory playerRebateFactory,
        ICashServiceClient cashServiceClient,
        IPlayerServiceClient playerServiceClient,
        ITgsService<PlayerAllAgentRebateRecord> playerAllAgentRebateService,
        ITgsService<PlayerAllAgentRebateContributor> playerAllAgentRebateContributorService,
        ITgsService<Transaction> transactionService,
        ILogger<PlayerAllAgentRebateTaskQueueProcessor> logger)
    {
        _taskQueue = taskQueue;
        _playerRebateFactory = playerRebateFactory;
        _cashServiceClient = cashServiceClient;
        _playerServiceClient = playerServiceClient;
        _playerAllAgentRebateService = playerAllAgentRebateService;
        _playerAllAgentRebateContributorService = playerAllAgentRebateContributorService;
        _transactionService = transactionService;
        _logger = logger;
    }

    public async Task ProcessQueue(CancellationToken cancellationToken)
    {
        var request = await _taskQueue.DequeueAsync(cancellationToken);

        _logger.LogInformation("player all_agent_rebate request: {request}", request);

        var playerAggregate =
            await _playerRebateFactory.GetPlayerAllAgentTurnoverWinLossAsync(request.ReportDate,
                request.PlayerCode);

        var playerRebates = GetRebates(playerAggregate, request.Conditions, request.ReportDate).ToArray();

        if (await _playerAllAgentRebateService.GetQueries()
                .AnyAsync(
                    x => x.PlayerCode == request.PlayerCode &&
                        x.From == request.ReportDate.ToUniversalTime() && x.To == request.ReportDate.ToUniversalTime(), cancellationToken))
        {
            _logger.LogInformation("save the all_agent_rebate already. request: {request}", request);
        }
        else
        {
            await _playerRebateFactory.CreateAllAgentPlayerRebatesAsync(playerRebates);
        }

        var childPlayers = GetChildPlayers(playerAggregate, playerRebates, request.ReportDate);

        await SendPlayerAllAgentRebatesAsync(childPlayers, request.ReportDate, request.PlayerCode, cancellationToken);
    }

    private static bool MatchConditionSetting(PlayerAllAgentTurnoverWinLossPlayerNumberModel model,
        IEnumerable<BrandAllAgentGameTypeConditionModel> conditions)
    {
        return conditions.Any(x => model.BrandId == x.BrandId &&
            (short)model.AllAgentGameType == (short)x.AllAgentGameType);
    }

    private static IEnumerable<PlayerAllAgentRebateModel> CheckRebates(
        PlayerAllAgentTurnoverWinLossPlayerNumberModel model,
        IEnumerable<BrandAllAgentGameTypeConditionModel> gameTypeConditions, DateTimeOffset reportDate)
    {
        var conditions = gameTypeConditions.Where(x => model.BrandId == x.BrandId &&
            ((short)model.AllAgentGameType == (short)x.AllAgentGameType)).ToArray();

        var validBetConditions = conditions.Where(x
            => x.HasValidBet && (short)x.AllAgentRebateType == (short)AllAgentRebateType.ValidBet).ToArray();

        var winLossConditions = conditions.Where(x
            => x.HasWinLoss && (short)x.AllAgentRebateType == (short)AllAgentRebateType.WinLoss).ToArray();

        var allAgentGameType = model.AllAgentGameType;
        var validBetRebateCondition = validBetConditions.Where(x => x.AllAgentGameType == allAgentGameType)
            .OrderByDescending(x => x.Rank).FirstOrDefault(x => model.PlayerNumber >= x.ActiveNumber &&
                model.Turnover >= x.NetProfit);
        var winLossRebateCondition = winLossConditions.Where(x => x.AllAgentGameType == allAgentGameType)
            .OrderByDescending(x => x.Rank).FirstOrDefault(x => model.PlayerNumber >= x.ActiveNumber &&
                model.WinLoss >= x.NetProfit);
        var actualTurnoverAmount = model.Turnover;
        var actualWinLossAmount = model.WinLoss;


        return new[]
        {
            new PlayerAllAgentRebateModel
            {
                BrandId = model.BrandId,
                PlayerCode = model.PlayerCode,
                AllAgentGameType = allAgentGameType,
                From = reportDate.ToUniversalTime(),
                To = reportDate.ToUniversalTime(),
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
                PlayerCode = model.PlayerCode,
                AllAgentGameType = allAgentGameType,
                From = reportDate.ToUniversalTime(),
                To = reportDate.ToUniversalTime(),
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

    private static IEnumerable<PlayerAllAgentRebateModel> GetRebates(
        IEnumerable<PlayerAllAgentTurnoverWinLossModel> models,
        IEnumerable<BrandAllAgentGameTypeConditionModel> gameTypeConditions, DateTimeOffset reportDate)
    {
        var playerTurnovers = 
            models
                .Select(x =>
                {
                    var allAgentGameType = x.GameType switch
                    {
                        GameType.Sportsbook or GameType.ESports => AllAgentGameType.Sportsbook,
                        GameType.VirtualSports                  => AllAgentGameType.VirtualSports,
                        GameType.LiveDealer                     => AllAgentGameType.LiveDealer,
                        GameType.CardGame                       => AllAgentGameType.CardGame,
                        GameType.Fishing or GameType.Arcade or GameType.Slot or GameType.TableGame => AllAgentGameType
                            .Slot,
                        GameType.Lottery => AllAgentGameType.Lottery,
                        _                => throw new ArgumentOutOfRangeException()
                    };

                    return new
                    {
                        x.BrandId,
                        x.PlayerUsername,
                        x.PlayerCode,
                        x.ChildPlayerCode,
                        x.Turnover,
                        x.WinLoss,
                        AllAgentGameType = allAgentGameType
                    };
                })
                .GroupBy(x => new { x.BrandId, x.PlayerCode, x.AllAgentGameType },
                (x, list) => new
                {
                    BrandId = x.BrandId,
                    PlayerCode = x.PlayerCode,
                    AllAgentGameType = x.AllAgentGameType,
                    list
                })
            .Select(x => new PlayerAllAgentTurnoverWinLossPlayerNumberModel
            {
                BrandId = x.BrandId,
                PlayerCode = x.PlayerCode,
                AllAgentGameType = x.AllAgentGameType,
                PlayerNumber = 
                    x.list.Select(l => l.ChildPlayerCode).Distinct().Count(),
                Turnover =
                    x.list.Sum(a => a.Turnover),
                WinLoss =
                    x.list.Sum(a => a.WinLoss)
            });

        return playerTurnovers
            .Where(x => MatchConditionSetting(x, gameTypeConditions))
            .SelectMany(x => CheckRebates(x, gameTypeConditions, reportDate)).ToArray();
    }

    private IEnumerable<PlayerAllAgentRebateContributorModel>
        GetChildPlayers(
        IEnumerable<PlayerAllAgentTurnoverWinLossModel> models,
        IEnumerable<PlayerAllAgentRebateModel> playerRebates,
        DateTimeOffset reportDate)
    {
        return models.Select(x =>
        {
            var allAgentGameType = x.GameType switch
            {
                GameType.Sportsbook or GameType.ESports => AllAgentGameType.Sportsbook,
                GameType.VirtualSports                  => AllAgentGameType.VirtualSports,
                GameType.LiveDealer                     => AllAgentGameType.LiveDealer,
                GameType.CardGame                       => AllAgentGameType.CardGame,
                GameType.Fishing or GameType.Arcade or GameType.Slot or GameType.TableGame => AllAgentGameType
                    .Slot,
                GameType.Lottery => AllAgentGameType.Lottery,
                _                => throw new ArgumentOutOfRangeException()
            };

            var turnoverCondition = playerRebates.FirstOrDefault(p
                => p.AllAgentGameType == allAgentGameType &&
                p.AllAgentRebateType == AllAgentRebateType.ValidBet);

            var winLossCondition = playerRebates.FirstOrDefault(p
                => p.AllAgentGameType == allAgentGameType &&
                p.AllAgentRebateType == AllAgentRebateType.WinLoss);

            return new
            {
                BrandId = x.BrandId,
                PlayerCode = x.PlayerCode,
                PlayerUsername = x.PlayerUsername,
                ChildPlayerCode = x.ChildPlayerCode,
                SportsTurnover = turnoverCondition?.AllAgentGameType == AllAgentGameType.Sportsbook ?
                    x.Turnover : 0M,
                VirtualSportsTurnover = turnoverCondition?.AllAgentGameType == AllAgentGameType.VirtualSports ?
                    x.Turnover : 0M,
                LiveDealerTurnover = turnoverCondition?.AllAgentGameType == AllAgentGameType.LiveDealer ?
                    x.Turnover : 0M,
                CardGameTurnover = turnoverCondition?.AllAgentGameType == AllAgentGameType.CardGame ?
                    x.Turnover : 0M,
                SlotTurnover = turnoverCondition?.AllAgentGameType == AllAgentGameType.Slot ?
                    x.Turnover : 0M,
                LotteryTurnover = turnoverCondition?.AllAgentGameType == AllAgentGameType.Lottery ?
                    x.Turnover : 0M,
                SportsTurnoverRebate = turnoverCondition?.AllAgentGameType == AllAgentGameType.Sportsbook ?
                    (turnoverCondition?.RebatePercentage ?? 0M) / 100 * x.Turnover : 0M,
                VirtualSportsTurnoverRebate = turnoverCondition?.AllAgentGameType == AllAgentGameType.VirtualSports ?
                    (turnoverCondition?.RebatePercentage ?? 0M) / 100 * x.Turnover : 0M,
                LiveDealerTurnoverRebate = turnoverCondition?.AllAgentGameType == AllAgentGameType.LiveDealer ?
                    (turnoverCondition?.RebatePercentage ?? 0M) / 100 * x.Turnover : 0M,
                CardGameTurnoverRebate = turnoverCondition?.AllAgentGameType == AllAgentGameType.CardGame ?
                    (turnoverCondition?.RebatePercentage ?? 0M) / 100 * x.Turnover : 0M,
                SlotTurnoverRebate = turnoverCondition?.AllAgentGameType == AllAgentGameType.Slot ?
                    (turnoverCondition?.RebatePercentage ?? 0M) / 100 * x.Turnover : 0M,
                LotteryTurnoverRebate = turnoverCondition?.AllAgentGameType == AllAgentGameType.Lottery ?
                    (turnoverCondition?.RebatePercentage ?? 0M) / 100 * x.Turnover : 0M,
                SportsWinLossRebate = winLossCondition?.AllAgentGameType == AllAgentGameType.Sportsbook ?
                    (winLossCondition?.RebatePercentage ?? 0M) / 100 * x.WinLoss : 0M,
                VirtualSportsWinLossRebate = winLossCondition?.AllAgentGameType == AllAgentGameType.VirtualSports ?
                    (winLossCondition?.RebatePercentage ?? 0M) / 100 * x.WinLoss : 0M,
                LiveDealerWinLossRebate = winLossCondition?.AllAgentGameType == AllAgentGameType.LiveDealer ?
                    (winLossCondition?.RebatePercentage ?? 0M) / 100 * x.WinLoss : 0M,
                CardGameWinLossRebate = winLossCondition?.AllAgentGameType == AllAgentGameType.CardGame ?
                    (winLossCondition?.RebatePercentage ?? 0M) / 100 * x.WinLoss : 0M,
                SlotWinLossRebate = winLossCondition?.AllAgentGameType == AllAgentGameType.Slot ?
                    (winLossCondition?.RebatePercentage ?? 0M) / 100 * x.WinLoss : 0M,
                LotteryWinLossRebate = winLossCondition?.AllAgentGameType == AllAgentGameType.Lottery ?
                    (winLossCondition?.RebatePercentage ?? 0M) / 100 * x.WinLoss : 0M,
            };
        })
            .GroupBy(x => new {x.BrandId,x.PlayerCode,x.PlayerUsername,x.ChildPlayerCode},
                (child, list) => new PlayerAllAgentRebateContributorModel
                {
                    BrandId = child.BrandId,
                    PlayerCode = child.PlayerCode,
                    PlayerUsername = child.PlayerUsername,
                    ChildPlayerCode = child.ChildPlayerCode,
                    From = reportDate.UtcDateTime,
                    To = reportDate.UtcDateTime,
                    SportsTurnover = list.Sum(s => s.SportsTurnover),
                    VirtualSportsTurnover = list.Sum(s => s.VirtualSportsTurnover),
                    LiveDealerTurnover = list.Sum(s => s.LiveDealerTurnover),
                    CardGameTurnover = list.Sum(s => s.CardGameTurnover),
                    SlotTurnover = list.Sum(s => s.SlotTurnover),
                    LotteryTurnover = list.Sum(s => s.LotteryTurnover),
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
                });
    }

    private async Task SendPlayerAllAgentRebatesAsync(
        IEnumerable<PlayerAllAgentRebateContributorModel> contributors,
        DateTimeOffset reportDate,
        string playerCode,
        CancellationToken cancellationToken)
    {
        var savedPlayerContributors = await _playerAllAgentRebateContributorService.GetQueries()
            .Where(x => x.From == reportDate.UtcDateTime && x.To == reportDate.UtcDateTime && x.PlayerCode == playerCode && x.SentOn == null)
            .Select(x => x.ChildPlayerCode)
            .ToArrayAsync(cancellationToken);

        var parentPlayer = await _playerServiceClient
            .GetPlayerPropertiesAsync(playerCode, cancellationToken).ConfigureAwait(false);
        var defaultWalletId = Guid.Parse(parentPlayer["DefaultWalletId"]);

        foreach (var rebate in contributors)
        {
            if (savedPlayerContributors.Contains(rebate.ChildPlayerCode))
            {
                continue;
            }

            var totalRebate = rebate.SportsTurnoverRebate +
                rebate.VirtualSportsTurnoverRebate +
                rebate.LiveDealerTurnoverRebate +
                rebate.CardGameTurnoverRebate +
                rebate.SlotTurnoverRebate +
                rebate.LotteryTurnoverRebate +
                rebate.SportsWinLossRebate +
                rebate.VirtualSportsWinLossRebate +
                rebate.LiveDealerWinLossRebate +
                rebate.CardGameWinLossRebate +
                rebate.SlotWinLossRebate +
                rebate.LotteryWinLossRebate;

            _logger.LogInformation("Player All Agent ParentPlayerCode:{rebate.PlayerCode} RebateAmount:{totalRebate}",
                rebate.PlayerCode, totalRebate);

            var id = await _playerRebateFactory.CreateAllAgentPlayerRebateContributorAsync(rebate)
                .ConfigureAwait(false);

            var player = await _playerServiceClient.GetPlayerInfoAsync(rebate.PlayerCode, cancellationToken)
                .ConfigureAwait(false);

            await Policy.Handle<Exception>()
                .RetryAsync(
                    3,
                    (exception, retryCount) => _logger.LogError(
                        "Player All Agent ParentPlayerCode:{rebate.PlayerCode} RebateAmount:{totalRebate} retryCount:{retryCount} Exception:{exception}",
                        rebate.PlayerCode,
                        totalRebate,
                        retryCount,
                        exception))
                .ExecuteAsync(
                    async () =>
                    {
                        await _playerRebateFactory
                            .CreatePlayerBucketsAsync(new CreatePlayerBucketRequest(
                                rebate.PlayerCode,
                                BucketType.AllAgent,
                                totalRebate,
                                id.ToString()))
                            .ConfigureAwait(false);
                    });

            await Policy.Handle<HttpRequestException>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(3),
                    (exception, time, retryCount, context) => _logger.LogError(
                        "Player All Agent externalId:{id} retryCount:{retryCount} Exception:{exception}",
                        id,
                        retryCount,
                        exception))
                .ExecuteAsync(
                    async () =>
                    {
                        if (!await _transactionService.GetQueries()
                                .AnyAsync(
                                    x => x.PlayerCode == playerCode
                                        && x.ExternalTransactionId == id.ToString(),
                                    cancellationToken: cancellationToken))
                        {
                            _ = await _cashServiceClient.RebateAsync(
                                defaultWalletId,
                                totalRebate,
                                player?.UserName,
                                id.ToString(), cancellationToken).ConfigureAwait(false);
                        }
                    });
        }
    }

}
