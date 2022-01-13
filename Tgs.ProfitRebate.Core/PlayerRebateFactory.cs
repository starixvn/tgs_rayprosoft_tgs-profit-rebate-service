using Microsoft.EntityFrameworkCore;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Models;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Core
{
    public class PlayerRebateFactory : IPlayerRebateFactory
    {
        private readonly ITgsService<PlayerAllAgentRebate> _allAgentPlayerRebateService;
        private readonly ITgsService<PlayerBucket> _playerBucketService;
        private readonly ITgsService<PlayerRebate> _playerRebateService;

        public PlayerRebateFactory(ITgsService<PlayerRebate> playerRebateService,
            ITgsService<PlayerBucket> playerBucketService,
            ITgsService<PlayerAllAgentRebate> allAgentPlayerRebateService)
        {
            _playerRebateService = playerRebateService;
            _playerBucketService = playerBucketService;
            _allAgentPlayerRebateService = allAgentPlayerRebateService;
        }

        public ValueTask CreateVipPlayerRebatesAsync(VipPlayerRebateModel[] models, DateTimeOffset? sentOn) =>
            _playerRebateService.CreateEntitiesAsync(models.Select(model => new PlayerRebate
            {
                BrandId = model.BrandId,
                AgentId = model.AgentId,
                PlayerCode = model.PlayerCode,
                ReportDate = model.ReportDate,
                CreatedOn = DateTimeOffset.Now,
                SentOn = sentOn,
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
            }).ToArray());

        public async ValueTask<PlayerAllAgentTurnoverWinLossModel[]>
            GetPlayerTurnoverWinLossAsync(DateTimeOffset reportDate) => await _allAgentPlayerRebateService.Repository
            .BrandAllAgents
            .Join(_allAgentPlayerRebateService.Repository.PlayerGameAggregates.Where(x => x.ReportDate == reportDate),
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
                    x.Aggregate.GameType
                },
                (x, list) => new PlayerAllAgentTurnoverWinLossModel
                {
                    BrandId = x.BrandId,
                    ParentPlayerCode = x.ParentPlayerCode,
                    GameType = x.GameType,
                    PlayerNumber = list.Select(l => l.Aggregate.PlayerCode).Distinct().Count(),
                    SportsTurnover =
                        list.Where(l => Consts.SportsGroup.Contains(l.Aggregate.GameType))
                            .Sum(x => x.Aggregate.Turnover),
                    VirtualSportsTurnover =
                        list.Where(l => l.Aggregate.GameType == GameType.VirtualSports)
                            .Sum(a => a.Aggregate.Turnover),
                    LiveDealerTurnover =
                        list.Where(l => l.Aggregate.GameType == GameType.LiveDealer).Sum(a => a.Aggregate.Turnover),
                    CardGameTurnover =
                        list.Where(l => l.Aggregate.GameType == GameType.CardGame).Sum(a => a.Aggregate.Turnover),
                    SlotTurnover =
                        list.Where(l => Consts.SlotGroup.Contains(l.Aggregate.GameType))
                            .Sum(a => a.Aggregate.Turnover),
                    LotteryTurnover = list.Where(a => a.Aggregate.GameType == GameType.Lottery)
                        .Sum(a => a.Aggregate.Turnover),
                    SportsWinLoss =
                        list.Where(l => Consts.SportsGroup.Contains(l.Aggregate.GameType))
                            .Sum(x => x.Aggregate.WinLoss),
                    VirtualSportsWinLoss =
                        list.Where(l => l.Aggregate.GameType == GameType.VirtualSports)
                            .Sum(a => a.Aggregate.WinLoss),
                    LiveDealerWinLoss =
                        list.Where(l => l.Aggregate.GameType == GameType.LiveDealer).Sum(a => a.Aggregate.WinLoss),
                    CardGameWinLoss =
                        list.Where(l => l.Aggregate.GameType == GameType.CardGame).Sum(a => a.Aggregate.WinLoss),
                    SlotWinLoss =
                        list.Where(l => Consts.SlotGroup.Contains(l.Aggregate.GameType))
                            .Sum(a => a.Aggregate.WinLoss),
                    LotteryWinLoss = list.Where(a => a.Aggregate.GameType == GameType.Lottery)
                        .Sum(a => a.Aggregate.WinLoss)
                })
            .ToArrayAsync();

        public async ValueTask CreatePlayerBucketsAsync(CreatePlayerBucketRequest[] requests)
        {
            var buckets = requests.Select(x => new PlayerBucket
            {
                PlayerCode = x.PlayerCode,
                BucketAmount = x.BucketAmount,
                BucketType = x.BucketType,
                IsFull = false,
                CreatedOn = DateTimeOffset.UtcNow
            }).ToArray();

            await _playerBucketService.CreateEntitiesAsync(buckets).ConfigureAwait(false);
        }

        public ValueTask CreateAllAgentPlayerRebatesAsync(PlayerAllAgentRebateModel[] models, DateTimeOffset? sentOn) =>
            _allAgentPlayerRebateService.CreateEntitiesAsync(models.Select(model => new PlayerAllAgentRebate
            {
                BrandId = model.BrandId,
                PlayerCode = model.PlayerCode,
                ReportDate = model.ReportDate,
                CreatedOn = DateTimeOffset.UtcNow,
                SentOn = sentOn,
                AllAgentGameType = model.AllAgentGameType,
                GameType = model.GameType,
                AllAgentRebateType = model.AllAgentRebateType,
                Rank = model.Rank,
                ActiveNumber = model.ActiveNumber,
                ConditionAmount = model.ConditionAmount,
                RebatePercentage = model.RebatePercentage,
                ActualConditionAmount = model.ActualConditionAmount,
                ActualPlayerNumber = model.ActualPlayerNumber,
                RebateAmount = model.RebateAmount
            }).ToArray());

        public async ValueTask<BrandAllAgentGameTypeConditionModel[]> GetBrandAllAgentGameTypeConditionsAsync()
        {
            return await _allAgentPlayerRebateService.Repository.BrandAllAgents.Join(
                    _allAgentPlayerRebateService.Repository.BrandAllAgentGameTypes, brand => brand.BrandId,
                    gameType => gameType.BrandId,
                    (brand, gameType) => new { brand.BrandId, GameType = gameType })
                .Join(_allAgentPlayerRebateService.Repository.BrandAllAgentGameTypeConditions,
                    group => new { group.BrandId, group.GameType.AllAgentGameType },
                    condition => new { condition.BrandId, condition.AllAgentGameType },
                    (group, condition) => new { group.BrandId, group.GameType, Condition = condition })
                .Where(x => x.GameType.Switch)
                .Where(x => x.GameType.HasValidBet || x.GameType.HasWinLoss)
                .Select(x => new BrandAllAgentGameTypeConditionModel
                {
                    BrandId = x.BrandId,
                    GameTypeSwitch = x.GameType.Switch,
                    HasValidBet = x.GameType.HasValidBet,
                    HasWinLoss = x.GameType.HasWinLoss,
                    AllAgentGameType = x.GameType.AllAgentGameType,
                    Rank = x.Condition.Rank,
                    ActiveNumber = x.Condition.ActiveNumber,
                    NetProfit = x.Condition.NetProfit,
                    RebatePercentage = x.Condition.RebatePercentage
                }).ToArrayAsync();
        }
    }
}