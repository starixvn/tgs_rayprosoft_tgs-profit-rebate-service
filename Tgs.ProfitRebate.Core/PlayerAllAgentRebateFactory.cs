using Microsoft.EntityFrameworkCore;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Entities;
using Tgs.Service.PlayerBuckets;

namespace Tgs.ProfitRebate.Core;

public class PlayerAllAgentRebateFactory : IPlayerAllAgentRebateFactory
{
	private readonly ITgsService<PlayerAllAgentRebateRecord> _allAgentPlayerRebateService;
    private readonly ITgsService<PlayerAllAgentRebateContributor> _playerAllAgentRebateContributorService;
	private readonly IPlayerBucketClient _playerBucketClient;

	public PlayerAllAgentRebateFactory(
		ITgsService<PlayerAllAgentRebateRecord> allAgentPlayerRebateService,
        ITgsService<PlayerAllAgentRebateContributor> playerAllAgentRebateContributorService,
        IPlayerBucketClient playerBucketClient)
	{
		_allAgentPlayerRebateService = allAgentPlayerRebateService ?? throw new ArgumentNullException(nameof(allAgentPlayerRebateService));
		_playerBucketClient = playerBucketClient ?? throw new ArgumentNullException(nameof(playerBucketClient));
        _playerAllAgentRebateContributorService = playerAllAgentRebateContributorService ?? throw new ArgumentNullException(nameof(playerAllAgentRebateContributorService));
	}

    public async ValueTask CreatePlayerBucketsAsync(CreatePlayerBucketRequest[] requests)
	{
		foreach (var request in requests)
			_ = await _playerBucketClient.CreateBucketAsync(
				request.PlayerCode,
				request.BucketAmount,
				request.BucketType,
				request.ExternalId,
                "").ConfigureAwait(false);
	}

    public ValueTask CreateAllAgentPlayerRebatesAsync(PlayerAllAgentRebateModel[] models)
        => _allAgentPlayerRebateService.CreateEntitiesAsync(models.Select(model => new PlayerAllAgentRebateRecord
        {
            Id = Guid.NewGuid(),
            BrandId = model.BrandId,
            PlayerCode = model.PlayerCode,
            ReportDate = model.ReportDate.UtcDateTime,
            CreatedOn = DateTime.UtcNow,
            AllAgentGameType = (short)model.AllAgentGameType,
            AllAgentRebateType = (short)model.AllAgentRebateType,
            Rank = model.Rank,
            ActiveNumber = model.ActiveNumber,
            ConditionAmount = model.ConditionAmount,
            RebatePercentage = model.RebatePercentage,
            ActualConditionAmount = model.ActualConditionAmount,
            ActualPlayerNumber = model.ActualPlayerNumber,
            RebateAmount = model.RebateAmount
        }).ToArray());

    public async ValueTask<BrandAllAgentGameTypeConditionModel[]> GetBrandAllAgentGameTypeConditionsAsync(
		Guid[] brandIds)
	{
		return await _allAgentPlayerRebateService.Repository.BrandAllAgents.Join(
				_allAgentPlayerRebateService.Repository.BrandAllAgentGameTypes, brand => brand.BrandId,
				gameType => gameType.BrandId,
				(brand, gameType) => new { brand.BrandId, GameType = gameType })
			.Join(_allAgentPlayerRebateService.Repository.BrandAllAgentGameTypeConditions,
				group => new { group.BrandId, AllAgentGameType = (int)group.GameType.AllAgentGameType },
				condition => new { condition.BrandId, condition.AllAgentGameType },
				(group, condition) => new { group.BrandId, group.GameType, Condition = condition })
			.Where(x => brandIds.Contains(x.BrandId))
			.Where(x => x.GameType.Switch)
			.Where(x => x.GameType.HasValidBet || x.GameType.HasWinLoss)
			.Select(x => new BrandAllAgentGameTypeConditionModel
			{
				BrandId = x.BrandId,
				GameTypeSwitch = x.GameType.Switch,
				HasValidBet = x.GameType.HasValidBet,
				HasWinLoss = x.GameType.HasWinLoss,
				AllAgentGameType = (AllAgentGameType)x.GameType.AllAgentGameType,
				AllAgentRebateType = (AllAgentRebateType)x.Condition.AllAgentRebateType,
				Rank = x.Condition.Rank,
				ActiveNumber = x.Condition.ActiveNumber,
				NetProfit = x.Condition.NetProfit,
				RebatePercentage = x.Condition.RebatePercentage
			}).ToArrayAsync();
	}

    public async ValueTask<(Guid, string)[]> GetValidAllAgentPlayerCodesAsync(DateTimeOffset reportDate)
    {
        return await _allAgentPlayerRebateService.Repository
            .BrandAllAgents
            .Join(
                _allAgentPlayerRebateService.Repository.PlayerGameRebateAggregates
                    .Where(x => x.ReportDate == reportDate.UtcDateTime),
                brand => brand.BrandId,
                aggregate => aggregate.BrandId,
                (brandAllAgent, aggregate) => new
                {
                    Aggregate = aggregate,
                    BrandAllAgent = brandAllAgent
                })
            .GroupJoin(_allAgentPlayerRebateService.Repository.BrandGames,
                group => new
                {
                    group.Aggregate.BrandId,
                    group.Aggregate.GameId
                },
                brandGame => new
                {
                    brandGame.BrandId,
                    brandGame.GameId
                },
                (aggregate, brandGame) => new
                {
                    aggregate.Aggregate,
                    BrandGame = brandGame
                })
            .SelectMany(x => x.BrandGame.DefaultIfEmpty(),
                (x, brandGame) => new
                {
                    x.Aggregate,
                    BrandGame = brandGame
                })
            .Where(x => x.BrandGame != null && x.BrandGame.Rebate)
            .Join(_allAgentPlayerRebateService.Repository.PlayerParents,
                group => group.Aggregate.PlayerCode,
                player => player.PlayerCode,
                (group, player) => new
                {
                    group.Aggregate,
                    group.BrandGame, Player = player
                })
            .GroupBy(
                x => new
                {
					x.Aggregate.BrandId,
                    x.Player.ParentPlayerCode
                },
                (x, list) => new
                {
					x.BrandId,
                    ParentPlayerCode = x.ParentPlayerCode
                })
            .Select(x => new ValueTuple<Guid, string>(x.BrandId, x.ParentPlayerCode))
            .ToArrayAsync();
    }

    public async ValueTask<PlayerAllAgentTurnoverWinLossModel[]> GetPlayerAllAgentTurnoverWinLossAsync(
        DateTimeOffset reportDate, string parentPlayerCode)
    {
        return await _allAgentPlayerRebateService.Repository
            .PlayerGameRebateAggregates
            .Where(x => x.ReportDate == reportDate.UtcDateTime)
            .Join(_allAgentPlayerRebateService.Repository.PlayerParents,
                group => group.PlayerCode,
                player => player.PlayerCode,
                (group, player) => new { Aggregate = group, Player = player })
            .Where(x => x.Player.ParentPlayerCode == parentPlayerCode)
            .GroupBy(
                x => new
                {
                    x.Aggregate.BrandId,
                    x.Player.ParentPlayerCode,
                    x.Aggregate.PlayerCode,
                    x.Aggregate.GameType
                },
                (x, list) => new PlayerAllAgentTurnoverWinLossModel
                {
                    BrandId = x.BrandId,
                    PlayerCode = x.ParentPlayerCode,
                    ChildPlayerCode = x.PlayerCode,
                    GameType = (GameType)x.GameType,
                    Turnover = list.Sum(x => x.Aggregate.Turnover),
                    WinLoss = list.Sum(a => a.Aggregate.WinLoss)
                })
            .ToArrayAsync()
            .ConfigureAwait(false);
    }

    public async ValueTask<Guid> CreateAllAgentPlayerRebateContributorAsync(PlayerAllAgentRebateContributorModel model)
    {
        var contributor = new PlayerAllAgentRebateContributor
        {
            Id = Guid.NewGuid(),
            BrandId = model.BrandId,
            PlayerCode = model.PlayerCode,
            ChildPlayerCode = model.ChildPlayerCode,
            ReportDate = model.ReportDate.UtcDateTime,
            CreatedOn = DateTime.UtcNow,
            SentOn = DateTime.UtcNow,
            SportsTurnover = model.SportsTurnover,
            VirtualSportsTurnover = model.VirtualSportsTurnover,
            LiveDealerTurnover = model.LiveDealerTurnover,
            CardGameTurnover = model.CardGameTurnover,
            SlotTurnover = model.SlotTurnover,
            LotteryTurnover = model.LotteryTurnover,
            SportsWinLoss = model.SportsWinLoss,
            VirtualSportsWinLoss = model.VirtualSportsWinLoss,
            LiveDealerWinLoss = model.LiveDealerWinLoss,
            CardGameWinLoss = model.CardGameWinLoss,
            SlotWinLoss = model.SlotWinLoss,
            LotteryWinLoss = model.LotteryWinLoss,
            SportsTurnoverRebate = model.SportsTurnoverRebate,
            VirtualSportsTurnoverRebate = model.VirtualSportsTurnoverRebate,
            LiveDealerTurnoverRebate = model.LiveDealerTurnoverRebate,
            CardGameTurnoverRebate = model.CardGameTurnoverRebate,
            SlotTurnoverRebate = model.SlotTurnoverRebate,
            LotteryTurnoverRebate = model.LotteryTurnoverRebate,
            SportsWinLossRebate = model.SportsWinLossRebate,
            VirtualSportsWinLossRebate = model.VirtualSportsWinLossRebate,
            LiveDealerWinLossRebate = model.LiveDealerWinLossRebate,
            CardGameWinLossRebate = model.CardGameWinLossRebate,
            SlotWinLossRebate = model.SlotWinLossRebate,
            LotteryWinLossRebate = model.LotteryWinLossRebate
        };

        await _playerAllAgentRebateContributorService.CreateEntityAsync(contributor);

        return contributor.Id;
    }
}
