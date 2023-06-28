using MediatR;
using Microsoft.EntityFrameworkCore;
using TGS.Core.Infrastructures.Extensions;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Core.Workers;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Entities;

namespace Tgs.ProfitRebate.Core.Commands;

public class
    CreatePlayerVipRebatesHandler : IRequestHandler<CreateVipPlayerRebatesRequest,
        MessageResponse>
{
    private readonly ITgsService<BrandGame> _brandGameService;
    private readonly ITgsService<Brand> _brandService;
    private readonly ITgsService<PlayerGameRebateAggregate> _playerGameAggregateService;
    private readonly ITgsService<Player> _playerService;
    private readonly IPlayerRebateTaskQueue _taskQueue;

    public CreatePlayerVipRebatesHandler(
        ITgsService<Brand> brandService,
        ITgsService<PlayerGameRebateAggregate> playerGameAggregateService,
        ITgsService<Player> playerService,
        ITgsService<BrandGame> brandGameService,
        IPlayerRebateTaskQueue taskQueue)
    {
        _brandService = brandService ?? throw new ArgumentNullException(nameof(brandService));
        _playerGameAggregateService = playerGameAggregateService ??
            throw new ArgumentNullException(nameof(playerGameAggregateService));
        _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
        _brandGameService =
            brandGameService ?? throw new ArgumentNullException(nameof(brandGameService));
        _taskQueue =
            taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
    }

    public async Task<MessageResponse> Handle(CreateVipPlayerRebatesRequest request,
        CancellationToken cancellationToken)
    {
        var previousDay = DateTimeOffset.Now.ToOffset(new(8, 0, 0)).AddDays(-1);
        var reportDate = new DateTimeOffset(previousDay.Year, previousDay.Month, previousDay.Day, 0, 0, 0,
            previousDay.Offset);

        if (request.Date.IsNotNullOrEmpty() &&
            DateTimeOffset.TryParse($"{request.Date} 00:00 +08:00", out var result))
        {
            reportDate = new(result.Year, result.Month, result.Day, 0, 0, 0,
                result.Offset);
        }

        var sportsGroup = Consts.SportsGroup.Select(x => (short)x).ToArray();
        var slotGroup = Consts.SlotGroup.Select(x => (short)x).ToArray();

        var playerRebates = await _playerGameAggregateService
            .GetQueries(q => q.Where(x => x.ReportDate == reportDate.ToUniversalTime()))
            .GroupJoin(_brandGameService.GetQueries(),
                aggregate => new { aggregate.BrandId, aggregate.GameId },
                brandGame => new { brandGame.BrandId, brandGame.GameId },
                (aggregate, brandGame) => new { aggregate, brandGame })
            .SelectMany(x => x.brandGame.DefaultIfEmpty(),
                (x, brandGame) => new { x.aggregate, brandGame })
            .Where(x => x.brandGame == null || x.brandGame.Rebate)
            .GroupBy(
                x => new
                {
                    x.aggregate.BrandId,
                    x.aggregate.PlayerCode,
                    x.aggregate.AgentId,
                    x.aggregate.VipCardGameRebatePercentage,
                    x.aggregate.VipLiveDealerRebatePercentage,
                    x.aggregate.VipLotteryRebatePercentage,
                    x.aggregate.VipSlotRebatePercentage,
                    x.aggregate.VipSportsRebatePercentage,
                    x.aggregate.VipVirtualSportsRebatePercentage
                },
                (x, list) => new VipPlayerRebateModel
                {
                    BrandId = x.BrandId,
                    PlayerCode = x.PlayerCode,
                    AgentId = x.AgentId,
                    ReportDate = reportDate.ToUniversalTime(),
                    VipCardGameRebatePercentage = x.VipCardGameRebatePercentage,
                    VipLiveDealerRebatePercentage = x.VipLiveDealerRebatePercentage,
                    VipLotteryRebatePercentage = x.VipLotteryRebatePercentage,
                    VipSlotRebatePercentage = x.VipSlotRebatePercentage,
                    VipSportsRebatePercentage = x.VipSportsRebatePercentage,
                    VipVirtualSportsRebatePercentage = x.VipVirtualSportsRebatePercentage,
                    SportsTurnover =
                        list.Where(l => sportsGroup.Contains(l.aggregate.GameType))
                            .Sum(x => x.aggregate.Turnover),
                    SportsRebateAmount = x.VipSportsRebatePercentage * list.Where(l =>
                            sportsGroup.Contains(l.aggregate.GameType))
                        .Sum(x => x.aggregate.Turnover) / 100M,
                    VirtualSportsTurnover =
                        list.Where(l => l.aggregate.GameType == (short)GameType.VirtualSports)
                            .Sum(a => a.aggregate.Turnover),
                    VirtualSportsRebateAmount =
                        x.VipVirtualSportsRebatePercentage *
                        list.Where(l => l.aggregate.GameType == (short)GameType.VirtualSports)
                            .Sum(a => a.aggregate.Turnover) / 100M,
                    LiveDealerTurnover =
                        list.Where(l => l.aggregate.GameType == (short)GameType.LiveDealer)
                            .Sum(a => a.aggregate.Turnover),
                    LiveDealerRebateAmount =
                        x.VipLiveDealerRebatePercentage *
                        list.Where(l => l.aggregate.GameType == (short)GameType.LiveDealer)
                            .Sum(a => a.aggregate.Turnover) / 100M,
                    CardGameTurnover =
                        list.Where(l => l.aggregate.GameType == (short)GameType.CardGame)
                            .Sum(a => a.aggregate.Turnover),
                    CardGameRebateAmount =
                        x.VipCardGameRebatePercentage *
                        list.Where(l => l.aggregate.GameType == (short)GameType.CardGame)
                            .Sum(a => a.aggregate.Turnover) /
                        100M,
                    SlotTurnover =
                        list.Where(l => slotGroup.Contains(l.aggregate.GameType))
                            .Sum(a => a.aggregate.Turnover),
                    SlotRebateAmount =
                        x.VipSlotRebatePercentage * list.Where(l =>
                            slotGroup.Contains(l.aggregate.GameType)).Sum(a => a.aggregate.Turnover) /
                        100M,
                    LotteryTurnover = list.Where(a => a.aggregate.GameType == (short)GameType.Lottery)
                        .Sum(a => a.aggregate.Turnover),
                    LotteryRebateAmount =
                        x.VipLotteryRebatePercentage * list
                            .Where(a => a.aggregate.GameType == (short)GameType.Lottery)
                            .Sum(a => a.aggregate.Turnover) / 100M,
                    RebateAmount = list.Sum(l => l.aggregate.VipRebateAmount)
                })
            .Where(x => x.RebateAmount > 0)
            .ToArrayAsync(cancellationToken);

        if (playerRebates.Length <= 0)
        {
            return new() { Valid = false, Message = "Has no player rebate data." };
        }

        var brandIds = playerRebates.Select(x => x.BrandId).Distinct();
        var brands = await _brandService.GetQueries().Where(x => brandIds.Contains(x.Id))
            .Select(x => new { x.Id, x.AutoSentPlayerBonus })
            .ToArrayAsync(cancellationToken);

        var sendPlayers = playerRebates
            .Join(brands, p => p.BrandId, b => b.Id, (player, brand) => new { player, brand }).Select(x
                => new { PlayerRebate = x.player, IsAutoSend = x.brand.AutoSentPlayerBonus });

        var players = await _playerService.GetQueries()
            .Where(x => sendPlayers.Select(p => p.PlayerRebate.PlayerCode).Contains(x.PlayerCode))
            .Select(x => new { x.BrandId, x.PlayerCode, x.Wallets, x.DefaultWalletId })
            .ToArrayAsync(cancellationToken);

        foreach (var player in sendPlayers.Join(players, pb => pb.PlayerRebate.PlayerCode, p => p.PlayerCode,
                     (pb, p) => new { Rebate = pb.PlayerRebate, pb.IsAutoSend, p }))
            await _taskQueue.QueuePlayerRebateAsync(new(cancellationToken)
            {
                BrandId = player.Rebate.BrandId,
                PlayerCode = player.Rebate.PlayerCode,
                AgentId = player.Rebate.AgentId,
                ReportDate = player.Rebate.ReportDate,
                VipCardGameRebatePercentage = player.Rebate.VipCardGameRebatePercentage,
                VipLiveDealerRebatePercentage = player.Rebate.VipLiveDealerRebatePercentage,
                VipLotteryRebatePercentage = player.Rebate.VipLotteryRebatePercentage,
                VipSlotRebatePercentage = player.Rebate.VipSlotRebatePercentage,
                VipSportsRebatePercentage = player.Rebate.VipSportsRebatePercentage,
                VipVirtualSportsRebatePercentage = player.Rebate.VipVirtualSportsRebatePercentage,
                SportsTurnover = player.Rebate.SportsTurnover,
                SportsRebateAmount = player.Rebate.SportsRebateAmount,
                VirtualSportsTurnover = player.Rebate.VirtualSportsTurnover,
                VirtualSportsRebateAmount = player.Rebate.VirtualSportsRebateAmount,
                LiveDealerTurnover = player.Rebate.LiveDealerTurnover,
                LiveDealerRebateAmount = player.Rebate.LiveDealerRebateAmount,
                CardGameTurnover = player.Rebate.CardGameTurnover,
                CardGameRebateAmount = player.Rebate.CardGameRebateAmount,
                SlotTurnover = player.Rebate.SlotTurnover,
                SlotRebateAmount = player.Rebate.SlotRebateAmount,
                LotteryTurnover = player.Rebate.LotteryTurnover,
                LotteryRebateAmount = player.Rebate.LotteryRebateAmount,
                RebateAmount = player.Rebate.RebateAmount,
                DefaultWalletId = player.p.DefaultWalletId,
                IsAutoSend = player.IsAutoSend
            });

        return new()
        {
            Valid = true
        };
    }
}