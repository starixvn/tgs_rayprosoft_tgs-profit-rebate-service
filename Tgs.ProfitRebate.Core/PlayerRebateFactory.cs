using Microsoft.EntityFrameworkCore;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Entities;
using Tgs.Service.PlayerBuckets;

namespace Tgs.ProfitRebate.Core;

public class PlayerRebateFactory : IPlayerRebateFactory
{
	private readonly ITgsService<PlayerAllAgentRebate> _allAgentPlayerRebateService;
	private readonly IPlayerBucketClient _playerBucketClient;
	private readonly ITgsService<PlayerRebate> _playerRebateService;

	public PlayerRebateFactory(
		ITgsService<PlayerRebate> playerRebateService,
		ITgsService<PlayerAllAgentRebate> allAgentPlayerRebateService,
		IPlayerBucketClient playerBucketClient)
	{
		_playerRebateService = playerRebateService ?? throw new ArgumentNullException(nameof(playerRebateService));
		_allAgentPlayerRebateService = allAgentPlayerRebateService ?? throw new ArgumentNullException(nameof(allAgentPlayerRebateService));
		_playerBucketClient = playerBucketClient ?? throw new ArgumentNullException(nameof(playerBucketClient));
	}

	public async ValueTask<PlayerAllAgentTurnoverWinLossModel[]>
		GetPlayerTurnoverWinLossAsync(DateTimeOffset reportDate)
	{
		var sportsGroup = Consts.SportsGroup.Select(x => (short)x).ToArray();
		var slotGroup = Consts.SlotGroup.Select(x => (short)x).ToArray();

		return await _allAgentPlayerRebateService.Repository
			.BrandAllAgents
			.Join(
				_allAgentPlayerRebateService.Repository.PlayerGameRebateAggregates.Where(x
					=> x.ReportDate == reportDate.ToUniversalTime()),
				brand => brand.BrandId, aggregate => aggregate.BrandId,
				(brandAllAgent, aggregate) => new { Aggregate = aggregate, BrandAllAgent = brandAllAgent })
			.GroupJoin(_allAgentPlayerRebateService.Repository.BrandGames,
				group => new { group.Aggregate.BrandId, group.Aggregate.GameId },
				brandGame => new { brandGame.BrandId, brandGame.GameId },
				(aggregate, brandGame) => new { aggregate.Aggregate, BrandGame = brandGame })
			.SelectMany(x => x.BrandGame.DefaultIfEmpty(),
				(x, brandGame) => new { x.Aggregate, BrandGame = brandGame })
			.Where(x => x.BrandGame != null && x.BrandGame.Rebate)
			.Join(_allAgentPlayerRebateService.Repository.PlayerParents,
				group => group.Aggregate.PlayerCode,
				player => player.PlayerCode,
				(group, player) => new { group.Aggregate, group.BrandGame, Player = player })
			.GroupBy(
				x => new
				{
					x.Aggregate.BrandId,
					x.Player.ParentPlayerCode,
					x.Player.PlayerCode,
					x.Aggregate.GameType
				},
				(x, list) => new PlayerAllAgentTurnoverWinLossModel
				{
					BrandId = x.BrandId,
					ParentPlayerCode = x.ParentPlayerCode,
					PlayerCode= x.PlayerCode,
					GameType = (GameType)x.GameType,
					//PlayerNumber = list.Select(l => l.Aggregate.PlayerCode).Distinct().Count(),
					SportsTurnover =
						list.Where(l => sportsGroup.Contains(l.Aggregate.GameType))
							.Sum(x => x.Aggregate.Turnover),
					VirtualSportsTurnover =
						list.Where(l => l.Aggregate.GameType == (short)GameType.VirtualSports)
							.Sum(a => a.Aggregate.Turnover),
					LiveDealerTurnover =
						list.Where(l => l.Aggregate.GameType == (short)GameType.LiveDealer)
							.Sum(a => a.Aggregate.Turnover),
					CardGameTurnover =
						list.Where(l => l.Aggregate.GameType == (short)GameType.CardGame)
							.Sum(a => a.Aggregate.Turnover),
					SlotTurnover =
						list.Where(l => slotGroup.Contains(l.Aggregate.GameType))
							.Sum(a => a.Aggregate.Turnover),
					LotteryTurnover = list.Where(a => a.Aggregate.GameType == (short)GameType.Lottery)
						.Sum(a => a.Aggregate.Turnover),
					SportsWinLoss =
						list.Where(l => sportsGroup.Contains(l.Aggregate.GameType))
							.Sum(x => x.Aggregate.WinLoss),
					VirtualSportsWinLoss =
						list.Where(l => l.Aggregate.GameType == (short)GameType.VirtualSports)
							.Sum(a => a.Aggregate.WinLoss),
					LiveDealerWinLoss =
						list.Where(l => l.Aggregate.GameType == (short)GameType.LiveDealer)
							.Sum(a => a.Aggregate.WinLoss),
					CardGameWinLoss =
						list.Where(l => l.Aggregate.GameType == (short)GameType.CardGame)
							.Sum(a => a.Aggregate.WinLoss),
					SlotWinLoss =
						list.Where(l => slotGroup.Contains(l.Aggregate.GameType))
							.Sum(a => a.Aggregate.WinLoss),
					LotteryWinLoss = list.Where(a => a.Aggregate.GameType == (short)GameType.Lottery)
						.Sum(a => a.Aggregate.WinLoss)
				})
			.ToArrayAsync().ConfigureAwait(false);
	}

    public ValueTask SendPlayerRebatesAsync(int rebateId, string playerCode) => throw new NotImplementedException();

    public async ValueTask CreatePlayerBucketsAsync(CreatePlayerBucketRequest[] requests)
	{
		foreach (var request in requests)
			_ = await _playerBucketClient.CreateBucketAsync(
				request.PlayerCode,
				request.BucketAmount,
				request.BucketType,
				request.ExternalId).ConfigureAwait(false);
	}

	public ValueTask CreateAllAgentPlayerRebateAsync(PlayerAllAgentRebateModel model)
		=> _allAgentPlayerRebateService.CreateEntityAsync(new PlayerAllAgentRebate
        {
            BrandId = model.BrandId,
            PlayerCode = model.PlayerCode,
            ReportDate = model.ReportDate.UtcDateTime,
            CreatedOn = DateTime.UtcNow,
            SentOn = DateTime.UtcNow,
            AllAgentGameType = (short)model.AllAgentGameType,
            GameType = (short)model.GameType,
            AllAgentRebateType = (short)model.AllAgentRebateType,
            Rank = model.Rank,
            ActiveNumber = model.ActiveNumber,
            ConditionAmount = model.ConditionAmount,
            RebatePercentage = model.RebatePercentage,
            ActualConditionAmount = model.ActualConditionAmount,
            ActualPlayerNumber = model.ActualPlayerNumber,
            RebateAmount = model.RebateAmount
        });

    public ValueTask CreateAllAgentPlayerRebatesAsync(PlayerAllAgentRebateModel[] models)
        => _allAgentPlayerRebateService.CreateEntitiesAsync(models.Select(model => new PlayerAllAgentRebate
        {
            BrandId = model.BrandId,
            PlayerCode = model.PlayerCode,
            ReportDate = model.ReportDate.UtcDateTime,
            CreatedOn = DateTime.UtcNow,
            AllAgentGameType = (short)model.AllAgentGameType,
            GameType = (short)model.GameType,
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

	public async ValueTask<PlayerRebate> CreateVipPlayerRebateAsync(VipPlayerRebateModel model, DateTimeOffset? sentOn)
	{
		var rebate = new PlayerRebate
		{
			BrandId = model.BrandId,
			AgentId = model.AgentId,
			PlayerCode = model.PlayerCode,
			ReportDate = model.ReportDate.UtcDateTime,
			CreatedOn = DateTime.UtcNow,
			SentOn = sentOn?.UtcDateTime,
			VipCardGameRebatePercentage = model.VipCardGameRebatePercentage,
			VipLiveDealerRebatePercentage = model.VipLiveDealerRebatePercentage,
			VipLotteryRebatePercentage = model.VipLotteryRebatePercentage,
			VipSlotRebatePercentage = model.VipSlotRebatePercentage,
			VipSportsRebatePercentage = model.VipSportsRebatePercentage,
			VipVirtualSportsRebatePercentage = model.VipVirtualSportsRebatePercentage,
			SportsTurnover = model.SportsTurnover,
			SportsRebateAmount = model.SportsRebateAmount,
			VirtualSportsTurnover = model.VirtualSportsTurnover,
			VirtualSportsRebateAmount = model.VirtualSportsRebateAmount,
			LiveDealerTurnover = model.LiveDealerTurnover,
			LiveDealerRebateAmount = model.LiveDealerRebateAmount,
			CardGameTurnover = model.CardGameTurnover,
			CardGameRebateAmount = model.CardGameRebateAmount,
			SlotTurnover = model.SlotTurnover,
			SlotRebateAmount = model.SlotRebateAmount,
			LotteryTurnover = model.LotteryTurnover,
			LotteryRebateAmount = model.LotteryRebateAmount,
			RebateAmount = model.RebateAmount
		};

		await _playerRebateService.CreateEntityAsync(rebate);

		return rebate;
	}

    public async ValueTask<(Guid, string)[]> GetValidAllAgentPlayerCodesAsync(DateTimeOffset reportDate)
    {
        return await _allAgentPlayerRebateService.Repository
            .BrandAllAgents
            .Join(
                _allAgentPlayerRebateService.Repository.PlayerGameRebateAggregates
                    .Where(x => x.ReportDate == reportDate.ToUniversalTime()),
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
        var sportsGroup = Consts.SportsGroup.Select(x => (short)x).ToArray();
        var slotGroup = Consts.SlotGroup.Select(x => (short)x).ToArray();

        return await _allAgentPlayerRebateService.Repository
            .PlayerGameRebateAggregates
            .Where(x => x.ReportDate == reportDate)
            .Join(_allAgentPlayerRebateService.Repository.PlayerParents,
                group => group.PlayerCode,
                player => player.PlayerCode,
                (group, player) => new { Aggregate = group, Player = player })
            .Where(x => x.Player.ParentPlayerCode == parentPlayerCode)
            .Join(_allAgentPlayerRebateService.Repository.Players,
                group => group.Aggregate.PlayerCode,
                player => player.PlayerCode,
                (group, player) => new { group.Aggregate ,group.Player, PlayerUsername = player.Username })
            .GroupBy(
                x => new
                {
                    x.PlayerUsername,
                    x.Aggregate.PlayerCode,
                    x.Aggregate.GameType,
                    x.Aggregate.Turnover,
                    x.Aggregate.WinLoss
                },
                (x, list) => new PlayerAllAgentTurnoverWinLossModel
                {
                    PlayerUsername = x.PlayerUsername,
                    PlayerCode = x.PlayerCode,
                    GameType = (GameType)x.GameType,
                    SportsTurnover =
                        list.Where(l => sportsGroup.Contains(l.Aggregate.GameType))
                            .Sum(x => x.Aggregate.Turnover),
                    VirtualSportsTurnover =
                        list.Where(l => l.Aggregate.GameType == (short)GameType.VirtualSports)
                            .Sum(a => a.Aggregate.Turnover),
                    LiveDealerTurnover =
                        list.Where(l => l.Aggregate.GameType == (short)GameType.LiveDealer)
                            .Sum(a => a.Aggregate.Turnover),
                    CardGameTurnover =
                        list.Where(l => l.Aggregate.GameType == (short)GameType.CardGame)
                            .Sum(a => a.Aggregate.Turnover),
                    SlotTurnover =
                        list.Where(l => slotGroup.Contains(l.Aggregate.GameType))
                            .Sum(a => a.Aggregate.Turnover),
                    LotteryTurnover = list.Where(a => a.Aggregate.GameType == (short)GameType.Lottery)
                        .Sum(a => a.Aggregate.Turnover),
                    SportsWinLoss =
                        list.Where(l => sportsGroup.Contains(l.Aggregate.GameType))
                            .Sum(x => x.Aggregate.WinLoss),
                    VirtualSportsWinLoss =
                        list.Where(l => l.Aggregate.GameType == (short)GameType.VirtualSports)
                            .Sum(a => a.Aggregate.WinLoss),
                    LiveDealerWinLoss =
                        list.Where(l => l.Aggregate.GameType == (short)GameType.LiveDealer)
                            .Sum(a => a.Aggregate.WinLoss),
                    CardGameWinLoss =
                        list.Where(l => l.Aggregate.GameType == (short)GameType.CardGame)
                            .Sum(a => a.Aggregate.WinLoss),
                    SlotWinLoss =
                        list.Where(l => slotGroup.Contains(l.Aggregate.GameType))
                            .Sum(a => a.Aggregate.WinLoss),
                    LotteryWinLoss = list.Where(a => a.Aggregate.GameType == (short)GameType.Lottery)
                        .Sum(a => a.Aggregate.WinLoss)
                })
            .ToArrayAsync()
            .ConfigureAwait(false);
    }
}
