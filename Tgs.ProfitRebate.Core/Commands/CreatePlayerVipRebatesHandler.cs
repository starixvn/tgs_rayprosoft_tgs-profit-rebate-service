using System.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tgs.CashService.Client;
using TGS.Core.Infrastructures.Extensions;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Models;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Core.Commands
{
    public class
        CreatePlayerVipRebatesHandler : IRequestHandler<CreateVipPlayerRebatesRequest,
            MessageResponse>
    {
        private readonly ITgsService<BrandGame> _brandGameRepository;
        private readonly ITgsService<Brand> _brandService;
        private readonly ICashServiceClient _cashServiceClient;
        private readonly ITgsService<PlayerGameAggregate> _playerGameAggregateService;
        private readonly ITgsService<PlayerRebate> _playerRebateService;
        private readonly ITgsService<Player> _playerService;
        private readonly IPlayerRebateFactory _vipPlayerRebateFactory;

        public CreatePlayerVipRebatesHandler(ITgsService<Brand> brandService,
            ITgsService<PlayerRebate> playerRebateService,
            ITgsService<PlayerGameAggregate> playerGameAggregateService,
            ICashServiceClient cashServiceClient,
            ITgsService<Player> playerService,
            ITgsService<BrandGame> brandGameRepository,
            IPlayerRebateFactory vipPlayerRebateFactory)
        {
            _brandService = brandService ?? throw new ArgumentNullException(nameof(brandService));
            _playerRebateService = playerRebateService ?? throw new ArgumentNullException(nameof(playerRebateService));
            _playerGameAggregateService = playerGameAggregateService ??
                throw new ArgumentNullException(nameof(playerGameAggregateService));
            _cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
            _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
            _brandGameRepository =
                brandGameRepository ?? throw new ArgumentNullException(nameof(brandGameRepository));
            _vipPlayerRebateFactory =
                vipPlayerRebateFactory ?? throw new ArgumentNullException(nameof(vipPlayerRebateFactory));
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

            if (await _playerRebateService.GetQueries()
                    .AnyAsync(x => x.ReportDate == reportDate.ToUniversalTime(), cancellationToken))
            {
                return new MessageResponse
                {
                    Valid = false,
                    Message = $"The rebates of {reportDate} have generated."
                };
            }

            var playerRebates = await _playerGameAggregateService
                .GetQueries(q => q.Where(x => x.ReportDate == reportDate.ToUniversalTime()))
                .GroupJoin(_brandGameRepository.GetQueries(),
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
                            list.Where(l => Consts.SportsGroup.Contains(l.aggregate.GameType))
                                .Sum(x => x.aggregate.Turnover),
                        SportsRebateAmount = x.VipSportsRebatePercentage * list.Where(l =>
                                Consts.SportsGroup.Contains(l.aggregate.GameType))
                            .Sum(x => x.aggregate.Turnover) / 100M,
                        VirtualSportsTurnover =
                            list.Where(l => l.aggregate.GameType == GameType.VirtualSports)
                                .Sum(a => a.aggregate.Turnover),
                        VirtualSportsRebateAmount =
                            x.VipVirtualSportsRebatePercentage *
                            list.Where(l => l.aggregate.GameType == GameType.VirtualSports)
                                .Sum(a => a.aggregate.Turnover) / 100M,
                        LiveDealerTurnover =
                            list.Where(l => l.aggregate.GameType == GameType.LiveDealer).Sum(a => a.aggregate.Turnover),
                        LiveDealerRebateAmount =
                            x.VipLiveDealerRebatePercentage *
                            list.Where(l => l.aggregate.GameType == GameType.LiveDealer)
                                .Sum(a => a.aggregate.Turnover) / 100M,
                        CardGameTurnover =
                            list.Where(l => l.aggregate.GameType == GameType.CardGame).Sum(a => a.aggregate.Turnover),
                        CardGameRebateAmount =
                            x.VipCardGameRebatePercentage *
                            list.Where(l => l.aggregate.GameType == GameType.CardGame).Sum(a => a.aggregate.Turnover) /
                            100M,
                        SlotTurnover =
                            list.Where(l => Consts.SlotGroup.Contains(l.aggregate.GameType))
                                .Sum(a => a.aggregate.Turnover),
                        SlotRebateAmount =
                            x.VipSlotRebatePercentage * list.Where(l =>
                                Consts.SlotGroup.Contains(l.aggregate.GameType)).Sum(a => a.aggregate.Turnover) /
                            100M,
                        LotteryTurnover = list.Where(a => a.aggregate.GameType == GameType.Lottery)
                            .Sum(a => a.aggregate.Turnover),
                        LotteryRebateAmount =
                            x.VipLotteryRebatePercentage * list.Where(a => a.aggregate.GameType == GameType.Lottery)
                                .Sum(a => a.aggregate.Turnover) / 100M,
                        RebateAmount = list.Sum(l => l.aggregate.VipRebateAmount)
                    })
                .Where(x => x.RebateAmount > 0)
                .ToArrayAsync(cancellationToken);

            if (playerRebates.Length <= 0)
            {
                return new() { Valid = false, Message = "Has no player rebate data." };
            }

            var brandIds = playerRebates.Select(x => x.BrandId);
            var brands = await _brandService.GetQueries().Where(x => brandIds.Contains(x.Id))
                .Select(x => new { x.Id, x.AutoSentPlayerBonus })
                .ToArrayAsync(cancellationToken);

            var autoSentPlayer = playerRebates
                .Join(brands, p => p.BrandId, b => b.Id, (player, brand) => new { player, brand })
                .Where(x => x.brand.AutoSentPlayerBonus).Select(x => x.player).ToArray();

            var playerBonusesNotSend = playerRebates
                .Join(brands, p => p.BrandId, b => b.Id, (player, brand) => new { player, brand })
                .Where(x => !x.brand.AutoSentPlayerBonus).Select(x => x.player).ToArray();

            var players = await _playerService.GetQueries()
                .Where(x => autoSentPlayer.Select(p => p.PlayerCode).Contains(x.PlayerCode))
                .Select(x => new { x.BrandId, x.PlayerCode, x.Wallets, x.DefaultWalletId })
                .ToArrayAsync(cancellationToken);

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            await _vipPlayerRebateFactory.CreateVipPlayerRebatesAsync(autoSentPlayer, DateTimeOffset.UtcNow);

            await _vipPlayerRebateFactory.CreateVipPlayerRebatesAsync(playerBonusesNotSend, null);
            await _vipPlayerRebateFactory.CreatePlayerBucketsAsync(autoSentPlayer.Join(players, pb => pb.PlayerCode,
                    p => p.PlayerCode,
                    (pb, p) => new { pb, p }).Select(x =>
                    new CreatePlayerBucketRequest(x.pb.PlayerCode, BucketType.Vip, x.pb.RebateAmount)).ToArray())
                .ConfigureAwait(false);

            scope.Complete();

            foreach (var playerBonus in autoSentPlayer.Join(players, pb => pb.PlayerCode, p => p.PlayerCode,
                         (pb, p) => new { pb, p }))
            {
                var response = await _cashServiceClient.RebateAsync(
                    playerBonus.p.DefaultWalletId,
                    playerBonus.pb.RebateAmount,
                    null,
                    null).ConfigureAwait(false);
            }

            return new MessageResponse
            {
                Valid = true
            };
        }
    }
}