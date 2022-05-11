using System.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using TGS.Core.Infrastructures.Extensions;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Entities;
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
        private readonly ITgsService<PlayerGameRebateAggregate> _playerGameAggregateService;
        private readonly ITgsService<PlayerRebate> _playerRebateService;
        private readonly ITgsService<Player> _playerService;
        private readonly IPlayerRebateFactory _vipPlayerRebateFactory;
        private readonly ILogger<CreatePlayerVipRebatesHandler> _logger;

        public CreatePlayerVipRebatesHandler(ITgsService<Brand> brandService,
            ITgsService<PlayerRebate> playerRebateService,
            ITgsService<PlayerGameRebateAggregate> playerGameAggregateService,
            ICashServiceClient cashServiceClient,
            ITgsService<Player> playerService,
            ITgsService<BrandGame> brandGameRepository,
            IPlayerRebateFactory vipPlayerRebateFactory,
            ILogger<CreatePlayerVipRebatesHandler> logger)
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
            _logger =
                logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MessageResponse> Handle(CreateVipPlayerRebatesRequest request,
            CancellationToken cancellationToken)
        {
            var brand = await _brandService.GetQueries().Where(x => x.Id == request.BrandId)
                .Select(x => new { x.Id, x.AutoSentPlayerBonus })
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (brand is null)
            {
                return new() { Valid = false, Message = "Can't find brand." };
            }

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
                    .AnyAsync(x => x.BrandId == request.BrandId && x.ReportDate == reportDate.ToUniversalTime(), cancellationToken).ConfigureAwait(false))
            {
                return new MessageResponse
                {
                    Valid = false,
                    Message = $"The rebates of {reportDate} have generated."
                };
            }

            var sportsGroup = Consts.SportsGroup.Select(x => (short)x).ToArray();
            var slotGroup = Consts.SlotGroup.Select(x => (short)x).ToArray();

            var playerRebates = await _playerGameAggregateService
                .GetQueries(q => q.Where(x => x.BrandId == request.BrandId && x.ReportDate == reportDate.ToUniversalTime()))
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
                        RebateAmount = list.Sum(l => l.aggregate.VipRebateAmount),

                    })
                .Where(x => x.RebateAmount > 0)
                .GroupJoin(_playerService.GetQueries().Where(x => x.BrandId == request.BrandId), group => group.PlayerCode, player => player.PlayerCode, (group, players) => new { group, players })
                .SelectMany(x => x.players.DefaultIfEmpty(), (group, player) => new { group.group, player })
                .Select(x => new VipPlayerRebateModel
                {
                    BrandId = x.group.BrandId,
                    PlayerCode = x.group.PlayerCode,
                    AgentId = x.group.AgentId,
                    ReportDate = x.group.ReportDate,
                    VipCardGameRebatePercentage = x.group.VipCardGameRebatePercentage,
                    VipLiveDealerRebatePercentage = x.group.VipLiveDealerRebatePercentage,
                    VipLotteryRebatePercentage = x.group.VipLotteryRebatePercentage,
                    VipSlotRebatePercentage = x.group.VipSlotRebatePercentage,
                    VipSportsRebatePercentage = x.group.VipSportsRebatePercentage,
                    VipVirtualSportsRebatePercentage = x.group.VipVirtualSportsRebatePercentage,
                    SportsTurnover = x.group.SportsTurnover,
                    VirtualSportsTurnover = x.group.VirtualSportsTurnover,
                    VirtualSportsRebateAmount = x.group.VirtualSportsRebateAmount,
                    LiveDealerTurnover = x.group.LiveDealerTurnover,
                    LiveDealerRebateAmount = x.group.LiveDealerRebateAmount,
                    CardGameTurnover = x.group.CardGameTurnover,
                    CardGameRebateAmount = x.group.CardGameRebateAmount,
                    SlotTurnover = x.group.SlotTurnover,
                    SlotRebateAmount = x.group.SlotRebateAmount,
                    LotteryTurnover = x.group.LotteryTurnover,
                    LotteryRebateAmount = x.group.LotteryRebateAmount,
                    RebateAmount = x.group.RebateAmount,
                    DefaultWalletId = x.player!.DefaultWalletId
                })
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);


            if (playerRebates.Length <= 0)
            {
                return new() { Valid = false, Message = "Has no player rebate data." };
            }

            var rebates = await _vipPlayerRebateFactory.CreateVipPlayerRebatesAsync(playerRebates, brand.AutoSentPlayerBonus ? DateTimeOffset.UtcNow : null);

            if (brand.AutoSentPlayerBonus)
            {
                await _vipPlayerRebateFactory.CreatePlayerBucketsAsync(playerRebates.Select(x =>
                        new CreatePlayerBucketRequest(x.PlayerCode, BucketType.Vip, x.RebateAmount)).ToArray())
                    .ConfigureAwait(false);
            }

            var players = await _playerService.GetQueries()
                .Where(x => x.BrandId == request.BrandId)
                .Where(x => playerRebates.Select(p => p.PlayerCode).Contains(x.PlayerCode))
                .Select(x => new { x.BrandId, x.PlayerCode, x.Wallets, x.DefaultWalletId })
                .ToArrayAsync(cancellationToken);

            foreach (var playerBonus in rebates.Join(players, pb => pb.PlayerCode, p => p.PlayerCode,
                         (pb, p) => new { pb, p }).Where(x => x.pb.RebateAmount > 0))
            {
                _logger.LogInformation($"player rebate: brand ({brand.Id}), player {playerBonus.p.PlayerCode}");

                _ = await _cashServiceClient.RebateAsync(
                    playerBonus.p.DefaultWalletId,
                    playerBonus.pb.RebateAmount,
                    null,
                    playerBonus.pb.Id.ToString()).ConfigureAwait(false);
            }

            return new MessageResponse
            {
                Valid = true
            };
        }
    }
}