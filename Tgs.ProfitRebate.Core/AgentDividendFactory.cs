using System.Text.Json;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TGS.Core.Infrastructures.Extensions;
using Tgs.Core.Infrastructures.Utility;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository;
using Tgs.ProfitRebate.Repository.Entities;
using Microsoft.Extensions.Logging;

namespace Tgs.ProfitRebate.Core;

public class AgentDividendFactory : IAgentDividendFactory
{
    private readonly ITgsService<AgentDividend> _agentDividendService;
    private readonly ITgsService<Agent> _agentService;
    private readonly IAgentWalletRepository _agentWalletRepository;
    private readonly ITgsService<Brand> _brandService;
    private readonly ITgsService<PlayerAggregate> _playerAggregateService;
    private readonly ITgsService<PlayerBonusAggregate> _playerBonusAggregateService;
    private readonly ITgsService<PlayerGameRebateAggregate> _playerGameAggregateService;
    private readonly ILogger<AgentDividendFactory> _logger;

    public AgentDividendFactory(
        ITgsService<Agent> agentService,
        ITgsService<Brand> brandService,
        ITgsService<AgentDividend> agentDividendService,
        ITgsService<PlayerAggregate> playerAggregateService,
        ITgsService<PlayerGameRebateAggregate> playerGameAggregateService,
        ITgsService<PlayerBonusAggregate> playerBonusAggregateService,
        IAgentWalletRepository agentWalletRepository,
        ILogger<AgentDividendFactory> logger)
    {
        _agentService = agentService;
        _brandService = brandService;
        _agentDividendService = agentDividendService;
        _playerAggregateService = playerAggregateService;
        _playerGameAggregateService = playerGameAggregateService;
        _agentWalletRepository = agentWalletRepository;
        _playerBonusAggregateService = playerBonusAggregateService;
        _logger = logger;
    }

    public (DateTimeOffset, DateTimeOffset) GetInterval(SettlementIntervalType type, DateTimeOffset startDate,
        int? dayOfMonth)
    {
        var date = startDate.AddDays(1);

        return type switch
        {
            SettlementIntervalType.Daily => (date, date),
            SettlementIntervalType.Weekly => (DateGenerator.GetFirstDayOfWeek(date),
                DateGenerator.GetLastDayOfWeek(date)),
            SettlementIntervalType.Monthly => (date,
                dayOfMonth == null
                    ? DateGenerator.GetLastDayOfMonth(date)
                    : DateGenerator.GetSpecifyDayOfMonth(startDate.AddMonths(1), dayOfMonth.Value!)),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public async IAsyncEnumerable<Brand[]> GetSettlementIntervalBrandsAsync(string brandCode, DateTimeOffset queryDate)
    {
        bool hasBrands;

        do
        {
            var brands = await _brandService.GetQueries().Where(brandCode is not null, x => x.Code == brandCode)
                .Where(x => queryDate.ToUniversalTime() > x.CurrentSettlementTo).AsTracking()
                .ToArrayAsync();

            hasBrands = brands.Any();

            yield return brands;
        } while (hasBrands);
    }

    public async ValueTask SaveAgentDividendDataAsync(Brand[] brands,
        List<CreateAgentDividendModel> createSendModels,
        List<AgentDividend> updateAgentDividends)
    {
        using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
            { Timeout = TimeSpan.FromMinutes(10) }, TransactionScopeAsyncFlowOption.Enabled);

        await CreateAgentDividendsAsync(createSendModels.ToArray());

        await _agentDividendService.UpdateEntitiesAsync(updateAgentDividends.ToArray());

        await _brandService.UpdateEntitiesAsync(brands);

        scope.Complete();
    }

    public async Task<(List<CreateAgentDividendModel>, int)>
        GetAgentDividendsAsync(Brand brand, int idNumber, CancellationToken cancellationToken)
    {
        var agents = await _agentService.GetQueries()
            .Where(x => x.BrandId == brand.Id)
            .Include(x => x.AgentDividendConditions)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var playerAggregate = await _agentService.GetQueries()
            .Where(x => x.BrandId == brand.Id)
            .GroupJoin(_playerAggregateService.GetQueries()
                    .Where(x => x.AgentId != null)
                    .Where(x => x.BrandId == brand.Id)
                    .Where(x => x.ReportDate >= brand.CurrentSettlementFrom &&
                        x.ReportDate <= brand.CurrentSettlementTo),
                a => a.Id,
                pa => pa.AgentId,
                (agent, list) => new
                {
                    agent,
                    list
                })
            .SelectMany(x => x.list.DefaultIfEmpty(),
                (x, agg) => new
                {
                    x.agent,
                    agg
                })
            .GroupBy(x => new
                {
                    ReportDate = x.agg == null ? (DateTimeOffset?)null : x.agg.ReportDate,
                    Hour = x.agg == null ? (int?)null : x.agg.Hour,
                    x.agent.BrandId,
                    AgentId = x.agent.Id,
                    x.agent.DividendPercentage,
                    AgentDividendPercentage = x.agg == null ? null : x.agg.AgentDividendPercentage,
                    AgentDividendPercentageTreeData = x.agg == null ? null : x.agg.AgentDividendPercentageTreeData
                },
                (groupItem, list) => new
                {
                    groupItem.ReportDate,
                    groupItem.Hour,
                    groupItem.AgentId,
                    groupItem.BrandId,
                    AgentDividendPercentage = groupItem.AgentDividendPercentage ?? groupItem.DividendPercentage,
                    groupItem.AgentDividendPercentageTreeData,
                    PromotionAmount = list.Sum(p => p.agg == null ? 0M : p.agg.PromotionAmount),
                    RebateAmount = list.Sum(p => p.agg == null ? 0M : p.agg.RebateAmount),
                    DepositAmount = list.Sum(p => p.agg == null ? 0M : p.agg.DepositAmount),
                    WithdrawalAmount = list.Sum(p => p.agg == null ? 0M : p.agg.WithdrawalAmount),
                    CompanyAdjustAmount = list.Sum(p => p.agg == null ? 0M : p.agg.CompanyAdjustAmount),
                    AgentAdjustAmount = list.Sum(p => p.agg == null ? 0M : p.agg.AgentAdjustAmount),
                    BonusAdjustAmount = list.Sum(p => p.agg == null ? 0M : p.agg.BonusAdjustAmount),
                    BonusAmount = list.Sum(p => p.agg == null ? 0M : p.agg.RebateAmount + p.agg.PromotionAmount),
                    BetAmount = list.Sum(p => p.agg == null ? 0M : p.agg.BetAmount),
                    ValidBetAmount = list.Sum(p => p.agg == null ? 0M : p.agg.ValidBetAmount),
                    OriginalTurnover = list.Sum(p => p.agg == null ? 0M : p.agg.OriginalTurnover),
                    Turnover = list.Sum(p => p.agg == null ? 0M : p.agg.Turnover),
                    WinLossAmount = list.Sum(p => p.agg == null ? 0M : p.agg.WinLossAmount),
                    NoIncludeNetProfitCompanyAdjustAmount =
                        list.Sum(p => p.agg == null ? 0M : p.agg.NoIncludeNetProfitCompanyAdjustAmount),
                    NoIncludeNetProfitAgentAdjustAmount =
                        list.Sum(p => p.agg == null ? 0M : p.agg.NoIncludeNetProfitAgentAdjustAmount),
                    NoIncludeNetProfitBonusAdjustAmount =
                        list.Sum(p => p.agg == null ? 0M : p.agg.NoIncludeNetProfitBonusAdjustAmount)
                })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var playerBonuses = await _playerBonusAggregateService.GetQueries()
            .Where(x => x.AgentId != null)
            .Where(x => x.BrandId == brand.Id)
            .Where(x => x.ReportDate >= brand.CurrentSettlementFrom &&
                x.ReportDate <= brand.CurrentSettlementTo)
            .GroupBy(x => new
                {
                    x.ReportDate,
                    x.Hour,
                    x.BrandId,
                    x.AgentId
                },
                (groupItem, list) => new
                {
                    ReportDate = (DateTimeOffset?)groupItem.ReportDate,
                    Hour = (int?)groupItem.Hour,
                    AgentId = groupItem.AgentId!.Value,
                    groupItem.BrandId,
                    PromotionAmount = 0M,
                    RebateAmount = 0M,
                    BonusAmount = list.Sum(p
                        => p.UpgradeAmount + p.SeasonAmount + p.BirthdayAmount),
                    DepositAmount = 0M,
                    WithdrawalAmount = 0M,
                    CompanyAdjustAmount = 0M,
                    AgentAdjustAmount = 0M,
                    BonusAdjustAmount = 0M,
                    BetAmount = 0M,
                    ValidBetAmount = 0M,
                    OriginalTurnover = 0M,
                    Turnover = 0M,
                    WinLossAmount = 0M,
                    NoIncludeNetProfitCompanyAdjustAmount = 0M,
                    NoIncludeNetProfitAgentAdjustAmount = 0M,
                    NoIncludeNetProfitBonusAdjustAmount = 0M
                })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var playerGameAggregate = await _playerGameAggregateService.GetQueries()
            .Where(x => x.AgentId != null)
            .Where(x => x.BrandId == brand.Id)
            .Where(x => x.ReportDate >= brand.CurrentSettlementFrom &&
                x.ReportDate <= brand.CurrentSettlementTo).GroupBy(x => new
                {
                    x.ReportDate,
                    x.Hour,
                    x.BrandId,
                    AgentId = x.AgentId!.Value
                },
                (agent, playerGames) => new
                {
                    ReportDate = (DateTimeOffset?)agent.ReportDate,
                    Hour = (int?)agent.Hour,
                    agent.AgentId,
                    agent.BrandId,
                    LiveDealerWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.LiveDealer)
                            .Sum(l => l.WinLoss),
                    SlotWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.Slot)
                            .Sum(l => l.WinLoss),
                    FishingWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.Fishing)
                            .Sum(l => l.WinLoss),
                    ArcadeWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.Arcade)
                            .Sum(l => l.WinLoss),
                    TableGameWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.TableGame)
                            .Sum(l => l.WinLoss),
                    LotteryWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.Lottery)
                            .Sum(l => l.WinLoss),
                    CardGameWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.CardGame)
                            .Sum(l => l.WinLoss),
                    SportsWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.Sportsbook)
                            .Sum(l => l.WinLoss),
                    ESportsWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.ESports)
                            .Sum(l => l.WinLoss),
                    VirtualSportsWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.VirtualSports)
                            .Sum(l => l.WinLoss),
                    WinLoss = playerGames
                        .Sum(l => l.WinLoss)
                })
            .ToArrayAsync(cancellationToken);

        var allAgentAggregate = playerAggregate.GroupJoin(playerBonuses,
                agg => new { agg.ReportDate, agg.Hour, agg.AgentId, agg.BrandId },
                aggBonus => new { aggBonus.ReportDate, aggBonus.Hour, aggBonus.AgentId, aggBonus.BrandId },
                (agg, list) => new
                {
                    agg,
                    list
                })
            .SelectMany(x => x.list.DefaultIfEmpty(),
                (x, bonusAgg) => new
                {
                    x.agg,
                    bonusAgg
                })
            .GroupBy(x => new
                {
                    x.agg.ReportDate,
                    x.agg.Hour,
                    x.agg.BrandId,
                    x.agg.AgentId,
                    x.agg.AgentDividendPercentage,
                    x.agg.AgentDividendPercentageTreeData
                },
                (groupItem, list) => new
                {
                    groupItem.ReportDate,
                    groupItem.Hour,
                    groupItem.AgentId,
                    groupItem.BrandId,
                    groupItem.AgentDividendPercentage,
                    groupItem.AgentDividendPercentageTreeData,
                    PromotionAmount = list.Sum(p => p.agg.PromotionAmount),
                    RebateAmount = list.Sum(p => p.agg.RebateAmount),
                    DepositAmount = list.Sum(p => p.agg.DepositAmount),
                    WithdrawalAmount = list.Sum(p => p.agg.WithdrawalAmount),
                    CompanyAdjustAmount = list.Sum(p => p.agg.CompanyAdjustAmount),
                    AgentAdjustAmount = list.Sum(p => p.agg.AgentAdjustAmount),
                    BonusAdjustAmount = list.Sum(p => p.agg.BonusAdjustAmount),
                    BonusAmount = list.Sum(p
                        => p.agg.BonusAmount + (p.bonusAgg?.BonusAmount ?? 0M)),
                    BetAmount = list.Sum(p => p.agg.BetAmount),
                    ValidBetAmount = list.Sum(p => p.agg.ValidBetAmount),
                    OriginalTurnover = list.Sum(p => p.agg.OriginalTurnover),
                    Turnover = list.Sum(p => p.agg.Turnover),
                    WinLossAmount = list.Sum(p => p.agg.WinLossAmount),
                    NoIncludeNetProfitCompanyAdjustAmount =
                        list.Sum(p => p.agg.NoIncludeNetProfitCompanyAdjustAmount),
                    NoIncludeNetProfitAgentAdjustAmount =
                        list.Sum(p => p.agg.NoIncludeNetProfitAgentAdjustAmount),
                    NoIncludeNetProfitBonusAdjustAmount =
                        list.Sum(p => p.agg.NoIncludeNetProfitBonusAdjustAmount)
                })
            .GroupJoin(playerGameAggregate, agg => new { agg.ReportDate, agg.Hour, agg.AgentId, agg.BrandId },
                aggGame => new { aggGame.ReportDate, aggGame.Hour, aggGame.AgentId, aggGame.BrandId },
                (agg, list) => new
                {
                    agg,
                    list
                })
            .SelectMany(x => x.list.DefaultIfEmpty(),
                (x, gameAgg) => new
                {
                    x.agg,
                    gameAgg
                })
            .GroupBy(x => new
                {
                    x.agg.ReportDate,
                    x.agg.Hour,
                    x.agg.BrandId,
                    x.agg.AgentId,
                    x.agg.AgentDividendPercentage,
                    x.agg.AgentDividendPercentageTreeData,
                    x.agg.PromotionAmount,
                    x.agg.RebateAmount,
                    x.agg.DepositAmount,
                    x.agg.WithdrawalAmount,
                    x.agg.CompanyAdjustAmount,
                    x.agg.AgentAdjustAmount,
                    x.agg.BonusAdjustAmount,
                    x.agg.BonusAmount,
                    x.agg.BetAmount,
                    x.agg.ValidBetAmount,
                    x.agg.OriginalTurnover,
                    x.agg.Turnover,
                    x.agg.WinLossAmount,
                    x.agg.NoIncludeNetProfitCompanyAdjustAmount,
                    x.agg.NoIncludeNetProfitAgentAdjustAmount,
                    x.agg.NoIncludeNetProfitBonusAdjustAmount
                },
                (groupItem, list) => new
                {
                    groupItem.ReportDate,
                    groupItem.Hour,
                    groupItem.AgentId,
                    groupItem.BrandId,
                    groupItem.AgentDividendPercentage,
                    groupItem.AgentDividendPercentageTreeData,
                    groupItem.PromotionAmount,
                    groupItem.RebateAmount,
                    groupItem.DepositAmount,
                    groupItem.WithdrawalAmount,
                    groupItem.CompanyAdjustAmount,
                    groupItem.AgentAdjustAmount,
                    groupItem.BonusAdjustAmount,
                    groupItem.BonusAmount,
                    groupItem.BetAmount,
                    groupItem.ValidBetAmount,
                    groupItem.OriginalTurnover,
                    groupItem.Turnover,
                    groupItem.WinLossAmount,
                    groupItem.NoIncludeNetProfitCompanyAdjustAmount,
                    groupItem.NoIncludeNetProfitAgentAdjustAmount,
                    groupItem.NoIncludeNetProfitBonusAdjustAmount,
                    NetProfit = groupItem.WinLossAmount
                        - (groupItem.BonusAdjustAmount - groupItem.NoIncludeNetProfitBonusAdjustAmount)
                        - groupItem.BonusAmount
                        - groupItem.WinLossAmount * brand.PlatformCharge / 100,
                    LiveDealerWinLoss = list.Sum(x => x.gameAgg?.LiveDealerWinLoss ?? 0M),
                    SlotWinLoss = list.Sum(x => x.gameAgg?.SlotWinLoss ?? 0M),
                    FishingWinLoss = list.Sum(x => x.gameAgg?.FishingWinLoss ?? 0M),
                    ArcadeWinLoss = list.Sum(x => x.gameAgg?.ArcadeWinLoss ?? 0M),
                    TableGameWinLoss = list.Sum(x => x.gameAgg?.TableGameWinLoss ?? 0M),
                    LotteryWinLoss = list.Sum(x => x.gameAgg?.LotteryWinLoss ?? 0M),
                    CardGameWinLoss = list.Sum(x => x.gameAgg?.CardGameWinLoss ?? 0M),
                    SportsWinLoss = list.Sum(x => x.gameAgg?.SportsWinLoss ?? 0M),
                    ESportsWinLoss = list.Sum(x => x.gameAgg?.ESportsWinLoss ?? 0M),
                    VirtualSportsWinLoss = list.Sum(x => x.gameAgg?.VirtualSportsWinLoss ?? 0M)
                })
            .GroupBy(x => new
                {
                    x.BrandId,
                    x.AgentId,
                    x.AgentDividendPercentage,
                    x.AgentDividendPercentageTreeData
                },
                (groupItem, list) => new CreateAgentDividendModel
                {
                    AgentId = groupItem.AgentId,
                    BrandId = groupItem.BrandId,
                    AutoSend = brand.AutoSentAgentBonus,
                    From = brand.CurrentSettlementFrom,
                    To = brand.CurrentSettlementTo,
                    AgentDividendPercentage = groupItem.AgentDividendPercentage,
                    AgentDividendPercentageTreeData = groupItem.AgentDividendPercentageTreeData,
                    PromotionAmount = list.Sum(x => x.PromotionAmount),
                    RebateAmount = list.Sum(x => x.RebateAmount),
                    DepositAmount = list.Sum(x => x.DepositAmount),
                    WithdrawalAmount = list.Sum(x => x.WithdrawalAmount),
                    CompanyAdjustAmount = list.Sum(x => x.CompanyAdjustAmount),
                    AgentAdjustAmount = list.Sum(x => x.AgentAdjustAmount),
                    BonusAdjustAmount = list.Sum(x => x.BonusAdjustAmount),
                    BonusAmount = list.Sum(x => x.BonusAmount),
                    BetAmount = list.Sum(x => x.BetAmount),
                    ValidBetAmount = list.Sum(x => x.ValidBetAmount),
                    OriginalTurnover = list.Sum(x => x.OriginalTurnover),
                    Turnover = list.Sum(x => x.Turnover),
                    WinLossAmount = list.Sum(x => x.WinLossAmount),
                    NoIncludeNetProfitCompanyAdjustAmount =
                        list.Sum(x => x.NoIncludeNetProfitCompanyAdjustAmount),
                    NoIncludeNetProfitAgentAdjustAmount =
                        list.Sum(x => x.NoIncludeNetProfitAgentAdjustAmount),
                    NoIncludeNetProfitBonusAdjustAmount =
                        list.Sum(x => x.NoIncludeNetProfitBonusAdjustAmount),
                    NetProfit = list.Sum(x => x.NetProfit),
                    LiveDealerWinLoss = list.Sum(x => x.LiveDealerWinLoss),
                    SlotWinLoss = list.Sum(x => x.SlotWinLoss),
                    FishingWinLoss = list.Sum(x => x.FishingWinLoss),
                    ArcadeWinLoss = list.Sum(x => x.ArcadeWinLoss),
                    TableGameWinLoss = list.Sum(x => x.TableGameWinLoss),
                    LotteryWinLoss = list.Sum(x => x.LotteryWinLoss),
                    CardGameWinLoss = list.Sum(x => x.CardGameWinLoss),
                    SportsWinLoss = list.Sum(x => x.SportsWinLoss),
                    ESportsWinLoss = list.Sum(x => x.ESportsWinLoss),
                    VirtualSportsWinLoss = list.Sum(x => x.VirtualSportsWinLoss),
                    CreatedOn = DateTimeOffset.UtcNow
                }).ToArray();

        var agentCurrentConditions = brand.AgentDividendType == (short)AgentDividendType.MultipleLayer
            ? await _playerAggregateService.GetQueries()
                .Where(x => x.AgentId != null)
                .Where(x => x.BrandId == brand.Id)
                .Where(x => x.ReportDate >= brand.CurrentSettlementFrom &&
                    x.ReportDate <= brand.CurrentSettlementTo)
                .GroupJoin(_playerBonusAggregateService.GetQueries(),
                    pa => new { pa.ReportDate, pa.Hour, pa.AgentId, pa.PlayerCode },
                    pb => new { pb.ReportDate, pb.Hour, pb.AgentId, pb.PlayerCode },
                    (agg, list) => new
                    {
                        agg,
                        list
                    })
                .SelectMany(x => x.list.DefaultIfEmpty(),
                    (x, bonusAgg) => new
                    {
                        x.agg,
                        bonusAgg
                    })
                .GroupBy(x => new { x.agg.AgentId, x.agg.PlayerCode }, (g, list) => new
                {
                    g.AgentId,
                    g.PlayerCode,
                    NetProfit = list.Sum(s => s.agg.WinLossAmount
                        - (s.agg.BonusAdjustAmount - s.agg.NoIncludeNetProfitBonusAdjustAmount)
                        - (s.agg.RebateAmount + s.agg.PromotionAmount + (s.bonusAgg == null
                            ? 0M
                            : s.bonusAgg.BirthdayAmount + s.bonusAgg.SeasonAmount + s.bonusAgg.UpgradeAmount))
                        - s.agg.WinLossAmount * brand.PlatformCharge / 100)
                })
                .GroupBy(x => x.AgentId, (agentId, items) => new AgentConditionModel
                {
                    AgentId = agentId!.Value,
                    ActiveCount = items.Count(),
                    NetProfit = items.Sum(s => s.NetProfit)
                }).ToArrayAsync(cancellationToken)
            : Array.Empty<AgentConditionModel>();

        var parentAgentDividends = brand.AgentDividendType == (short)AgentDividendType.MultipleLayer
            ? allAgentAggregate.SelectMany(x =>
            {
                x.Id = idNumber += 1;

                return GetAgentDividendsByMultipleLayer(x.Id, agents, agentCurrentConditions,
                    x.AgentId,
                    x.NetProfit,
                    brand.CheckDividendActiveCount);
            }).ToArray()
            : allAgentAggregate
                .SelectMany(x =>
                {
                    x.Id = idNumber += 1;

                    var agentDividendPercentage = x.AgentDividendPercentageTreeData is null
                        ? null
                        : JsonSerializer.Deserialize<AgentDividendPercentageData[]>(x.AgentDividendPercentageTreeData);

                    var bonusAdjustAmount = x.BonusAdjustAmount - x.NoIncludeNetProfitBonusAdjustAmount;

                    _logger.LogInformation("agentId: {agentId}, agentTree: {agentTree}", x.AgentId, x.AgentDividendPercentageTreeData);

                    return x.AgentDividendPercentageTreeData is null
                        ? GetAgentDividends(
                            x.Id,
                            agents,
                            x.AgentId,
                            x.BonusAmount,
                            bonusAdjustAmount,
                            x.NetProfit)
                        : GetAgentDividends(
                            x.Id,
                            agentDividendPercentage!,
                            x.AgentId,
                            x.BonusAmount,
                            bonusAdjustAmount,
                            x.NetProfit);
                }).ToArray();

        var allAgent = allAgentAggregate.GroupBy(x => new { x.AgentId, x.BrandId, x.AgentDividendPercentage },
                (x, list) =>
                    new CreateAgentDividendModel
                    {
                        Id = allAgentAggregate.First(a => a.AgentId == x.AgentId &&
                                                          a.BrandId == x.BrandId &&
                                                          a.AgentDividendPercentage == x.AgentDividendPercentage)
                            .Id,
                        AgentId = x.AgentId,
                        BrandId = x.BrandId,
                        AutoSend = brand.AutoSentAgentBonus,
                        From = brand.CurrentSettlementFrom,
                        To = brand.CurrentSettlementTo,
                        AgentDividendPercentage = x.AgentDividendPercentage,
                        PromotionAmount = list.Sum(x => x.PromotionAmount),
                        RebateAmount = list.Sum(x => x.RebateAmount),
                        DepositAmount = list.Sum(x => x.DepositAmount),
                        WithdrawalAmount = list.Sum(x => x.WithdrawalAmount),
                        CompanyAdjustAmount = list.Sum(x => x.CompanyAdjustAmount),
                        AgentAdjustAmount = list.Sum(x => x.AgentAdjustAmount),
                        BonusAdjustAmount = list.Sum(x => x.BonusAdjustAmount),
                        BonusAmount = list.Sum(x => x.BonusAmount),
                        BetAmount = list.Sum(x => x.BetAmount),
                        ValidBetAmount = list.Sum(x => x.ValidBetAmount),
                        OriginalTurnover = list.Sum(x => x.OriginalTurnover),
                        Turnover = list.Sum(x => x.Turnover),
                        WinLossAmount = list.Sum(x => x.WinLossAmount),
                        NoIncludeNetProfitCompanyAdjustAmount =
                            list.Sum(x => x.NoIncludeNetProfitCompanyAdjustAmount),
                        NoIncludeNetProfitAgentAdjustAmount =
                            list.Sum(x => x.NoIncludeNetProfitAgentAdjustAmount),
                        NoIncludeNetProfitBonusAdjustAmount =
                            list.Sum(x => x.NoIncludeNetProfitBonusAdjustAmount),
                        NetProfit = list.Sum(x => x.NetProfit),
                        LiveDealerWinLoss = list.Sum(x => x.LiveDealerWinLoss),
                        SlotWinLoss = list.Sum(x => x.SlotWinLoss),
                        FishingWinLoss = list.Sum(x => x.FishingWinLoss),
                        ArcadeWinLoss = list.Sum(x => x.ArcadeWinLoss),
                        TableGameWinLoss = list.Sum(x => x.TableGameWinLoss),
                        LotteryWinLoss = list.Sum(x => x.LotteryWinLoss),
                        CardGameWinLoss = list.Sum(x => x.CardGameWinLoss),
                        SportsWinLoss = list.Sum(x => x.SportsWinLoss),
                        ESportsWinLoss = list.Sum(x => x.ESportsWinLoss),
                        VirtualSportsWinLoss = list.Sum(x => x.VirtualSportsWinLoss),
                        CreatedOn = DateTimeOffset.UtcNow
                    })
            .ToArray();

        foreach (var dividend in allAgent)
            if (brand.AgentDividendType == (short)AgentDividendType.MultipleLayer)
            {
                dividend.AgentDividendAmount =
                    parentAgentDividends.Where(p => p.AgentId == dividend.AgentId)
                        .Sum(s => s.Dividend) -
                    (dividend.AgentAdjustAmount - dividend.NoIncludeNetProfitAgentAdjustAmount);

                dividend.AgentDividendMultipleLayerModels =
                    parentAgentDividends.Where(p => p.AgentId == dividend.AgentId).ToArray();
            }
            else
            {
                dividend.AgentDividendAmount =
                    parentAgentDividends.Where(p
                            => p.AgentId == dividend.AgentId &&
                            p.AgentDividendPercentage == dividend.AgentDividendPercentage)
                        .Sum(s => s.Dividend) -
                    (dividend.AgentAdjustAmount - dividend.NoIncludeNetProfitAgentAdjustAmount);

                dividend.AgentDividendMultipleLayerModels =
                    parentAgentDividends.Where(p
                            => p.AgentId == dividend.AgentId &&
                            p.AgentDividendPercentage == dividend.AgentDividendPercentage)
                        .ToArray();
            }

        allAgent = allAgent.Where(x => x.DepositAmount != 0M || x.WithdrawalAmount != 0M || x.AgentAdjustAmount != 0M ||
                                       x.BonusAmount != 0M || x.BetAmount != 0M || x.Turnover != 0M || x.WinLossAmount != 0M ||
                                       x.NetProfit != 0M || x.BonusAdjustAmount != 0M || x.AgentDividendPercentage != 0M ||
                                       x.AgentDividendAmount != 0M ||
                                       x.PromotionAmount != 0M || x.RebateAmount != 0M || x.OriginalTurnover != 0M ||
                                       x.ValidBetAmount != 0M).ToArray();

        return (allAgent.ToList(),
            allAgent.Any() ? allAgent.Max(x => x.Id) : idNumber);
    }

    public async Task<(List<CreateAgentDividendModel> ,List<AgentDividend>)> GetNoSendDividendsAsync(List<CreateAgentDividendModel> agentDividends, CancellationToken cancellationToken)
    {
        var agentIds = agentDividends.Where(x => x.AgentDividendAmount >= 0M).Select(x => x.AgentId).ToArray();

        var noSendDividends = await _agentDividendService.GetQueries()
            .Where(x => agentIds.Contains(x.AgentId) && x.SentOn == null)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var deductedNegativeDividends = new Dictionary<Guid, decimal>();

        foreach (var dividend in agentDividends)
        {
            var agentNoSendDividends = noSendDividends.Where(x => x.AgentId == dividend.AgentId).ToArray();

            var haveDeductedNegativeDividend = deductedNegativeDividends.TryGetValue(dividend.AgentId, out var deductedNegativeDividend);

            var agentNoSendDividend = haveDeductedNegativeDividend ? deductedNegativeDividend : agentNoSendDividends.Sum(x => x.AgentDividendAmount);

            if (agentNoSendDividend < 0)
            {
                var agentDividendAmount = agentDividends.Where(x => x.AgentId == dividend.AgentId)
                    .Sum(x => x.AgentDividendAmount);

                if (agentDividendAmount + agentNoSendDividend > 0M)
                {
                    dividend.BeforeDividendAmount = dividend.AgentDividendAmount;

                    deductedNegativeDividends[dividend.AgentId] = dividend.AgentDividendAmount + agentNoSendDividend;

                    if (dividend.AgentDividendAmount + agentNoSendDividend <= 0M)
                    {
                        dividend.AgentDividendAmount = 0M;
                    }
                    else
                    {
                        dividend.AgentDividendAmount += agentNoSendDividend;
                    }

                    foreach (var negativeDividend in agentNoSendDividends)
                    {
                        negativeDividend.ReferencedAgentDividendId = dividend.Id;
                        negativeDividend.SentOn = DateTime.UtcNow;
                    }
                }
                else
                {
                    dividend.AutoSend = false;
                }
            }
        }

        return (agentDividends,noSendDividends.Where(x => x.ReferencedAgentDividendId is not null).ToList());
    }

    public Task SetSentOnAsync(IEnumerable<int> agentDividendId, CancellationToken cancellationToken)
    {
        return _agentDividendService.GetQueries()
            .Where(a => agentDividendId.Contains(a.Id))
            .ExecuteUpdateAsync(e => e.SetProperty(s => s.SentOn, DateTime.UtcNow), cancellationToken);
    }

    private static decimal GetAgentDividendPercentage(AgentDividendCondition[] agentDividendCondition,
        AgentConditionModel? currentCondition, bool checkDividendActiveCount)
        => agentDividendCondition.OrderByDescending(x => x.Rank).Where(!checkDividendActiveCount,
                x => currentCondition?.NetProfit >= x.NetProfit)
            .Where(checkDividendActiveCount, x => currentCondition?.ActiveCount >= x.ActiveNumber &&
                currentCondition.NetProfit >= x.NetProfit).Select(x => x.DividendPercentage).FirstOrDefault();

    private static IEnumerable<AgentDividendMultipleLayerModel> GetAgentDividendsByMultipleLayer(
        int id,
        Agent[] agents, AgentConditionModel[] agentCurrentConditions, Guid agentId,
        decimal netProfit, bool checkDividendActiveCount)
    {
        var agent = agents.First(x => x.Id == agentId);
        var currentAgentId = agentId;
        var providerAgentId = agent.Id;
        var layer = agent.Id.ToString();

        var agentDividendPercentage = GetAgentDividendPercentage(agent.AgentDividendConditions.ToArray(),
            agentCurrentConditions.FirstOrDefault(x => x.AgentId == currentAgentId), checkDividendActiveCount);

        yield return new()
        {
            AgentId = currentAgentId,
            DividendProviderAgentId = providerAgentId,
            DividendProviderId = id,
            AgentDividendPercentage = agentDividendPercentage,
            ActualDividendPercentage = agentDividendPercentage,
            Layer = layer,
            Dividend = (netProfit * agentDividendPercentage / 100M).Truncate(2)
        };
    }

    private static IEnumerable<AgentDividendMultipleLayerModel> GetAgentDividends(
        int id,
        AgentDividendPercentageData[] agentDividendPercentages,
        Guid agentId,
        decimal bonusAmount,
        decimal bonusAdjustAmount,
        decimal netProfit)
    {
        bool hasParentAgentId;

        var agent = agentDividendPercentages.First(x => x.AgentId == agentId);
        var currentAgentId = agentId;
        var providerAgentId = agent.AgentId;
        var childDividendPercentage = 0M;
        var layer = agent.AgentId.ToString();

        do
        {
            var agentDividendPercentage = agent.DividendPercentage;
            var actualDividendPercentage = agentDividendPercentage - childDividendPercentage;

            yield return new()
            {
                AgentId = currentAgentId,
                DividendProviderAgentId = providerAgentId,
                DividendProviderId = id,
                AgentDividendPercentage = agentDividendPercentage,
                ActualDividendPercentage = actualDividendPercentage,
                Layer = layer,
                AgentCoveredBonus = (bonusAmount * actualDividendPercentage / 100M).Truncate(2),
                AgentCoveredBonusAdjust = (bonusAdjustAmount * actualDividendPercentage / 100M).Truncate(2),
                Dividend = (netProfit * actualDividendPercentage / 100M).Truncate(2)
            };

            hasParentAgentId = agent.ParentAgentId != null;

            if (!hasParentAgentId)
            {
                continue;
            }

            childDividendPercentage = agentDividendPercentage;

            var parentAgent = agentDividendPercentages.First(x => x.AgentId == agent.ParentAgentId);
            currentAgentId = parentAgent.AgentId;

            layer = $"{currentAgentId},{layer}";

            agent = parentAgent;
        } while (hasParentAgentId);
    }

    private static IEnumerable<AgentDividendMultipleLayerModel> GetAgentDividends(
        int id,
        Agent[] agents,
        Guid agentId,
        decimal bonusAmount,
        decimal bonusAdjustAmount,
        decimal netProfit)
    {
        bool hasParentAgentId;

        var agent = agents.First(x => x.Id == agentId);
        var currentAgentId = agentId;
        var providerAgentId = agent.Id;
        var childDividendPercentage = 0M;
        var layer = agent.Id.ToString();

        do
        {
            var agentDividendPercentage = agent.DividendPercentage;
            var actualDividendPercentage = agentDividendPercentage - childDividendPercentage;

            yield return new()
            {
                AgentId = currentAgentId,
                DividendProviderAgentId = providerAgentId,
                DividendProviderId = id,
                AgentDividendPercentage = agentDividendPercentage,
                ActualDividendPercentage = actualDividendPercentage,
                Layer = layer,
                AgentCoveredBonus = (bonusAmount * actualDividendPercentage / 100M).Truncate(2),
                AgentCoveredBonusAdjust = (bonusAdjustAmount * actualDividendPercentage / 100M).Truncate(2),
                Dividend = (netProfit * actualDividendPercentage / 100M).Truncate(2)
            };

            hasParentAgentId = agent.ParentAgentId != null;

            if (!hasParentAgentId)
            {
                continue;
            }

            childDividendPercentage = agentDividendPercentage;

            var parentAgent = agents.First(x => x.Id == agent.ParentAgentId);
            currentAgentId = parentAgent.Id;

            layer = $"{currentAgentId},{layer}";

            agent = parentAgent;
        } while (hasParentAgentId);
    }

    private ValueTask CreateAgentDividendsAsync(CreateAgentDividendModel[] models) =>
        _agentDividendService.CreateEntitiesAsync(models.Select(model => new AgentDividend
        {
            Id = model.Id,
            BrandId = model.BrandId,
            AgentId = model.AgentId,
            From = model.From.UtcDateTime,
            To = model.To.UtcDateTime,
            AgentDividendPercentage = model.AgentDividendPercentage,
            PromotionAmount = model.PromotionAmount,
            RebateAmount = model.RebateAmount,
            DepositAmount = model.DepositAmount,
            WithdrawalAmount = model.WithdrawalAmount,
            CompanyAdjustAmount = model.CompanyAdjustAmount,
            AgentAdjustAmount = model.AgentAdjustAmount,
            BonusAdjustAmount = model.BonusAdjustAmount,
            BonusAmount = model.BonusAmount,
            BetAmount = model.BetAmount,
            ValidBetAmount = model.ValidBetAmount,
            OriginalTurnover = model.OriginalTurnover,
            Turnover = model.Turnover,
            WinLossAmount = model.WinLossAmount,
            NoIncludeNetProfitCompanyAdjustAmount = model.NoIncludeNetProfitCompanyAdjustAmount,
            NoIncludeNetProfitAgentAdjustAmount = model.NoIncludeNetProfitAgentAdjustAmount,
            NoIncludeNetProfitBonusAdjustAmount = model.NoIncludeNetProfitBonusAdjustAmount,
            NetProfit = model.NetProfit,
            LiveDealerWinLoss = model.LiveDealerWinLoss,
            SlotWinLoss = model.SlotWinLoss,
            FishingWinLoss = model.FishingWinLoss,
            ArcadeWinLoss = model.ArcadeWinLoss,
            TableGameWinLoss = model.TableGameWinLoss,
            LotteryWinLoss = model.LotteryWinLoss,
            CardGameWinLoss = model.CardGameWinLoss,
            SportsWinLoss = model.SportsWinLoss,
            ESportsWinLoss = model.ESportsWinLoss,
            VirtualSportsWinLoss = model.VirtualSportsWinLoss,
            AgentDividendAmount = model.AgentDividendAmount,
            CreatedOn = DateTime.UtcNow,
            SentOn = null,
            BeforeDividendAmount = model.BeforeDividendAmount,
            ReferencedAgentDividendId = model.ReferencedAgentDividendId,
            AgentDividendMultipleLayers = model.AgentDividendMultipleLayerModels.Select(x
                => new AgentDividendMultipleLayer
                {
                    AgentId = x.AgentId,
                    AgentDividendId = model.Id,
                    AgentDividendPercentage = x.AgentDividendPercentage,
                    ActualDividendPercentage = x.ActualDividendPercentage,
                    AgentCoveredBonus = x.AgentCoveredBonus,
                    AgentCoveredBonusAdjust = x.AgentCoveredBonusAdjust,
                    DividendProviderAgentId = x.DividendProviderAgentId,
                    DividendProviderId = x.DividendProviderId,
                    Layer = x.Layer,
                    Dividend = x.Dividend
                }).ToArray()
        }).ToArray());
}