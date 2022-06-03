using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using TGS.Core.Infrastructures.Extensions;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Entities;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Core.Workers
{
    public class PlayerRebateBackground : BackgroundService
    {
        private readonly ILogger<PlayerRebateBackground> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPlayerRebateTaskQueue _taskQueue;

        public PlayerRebateBackground(IPlayerRebateTaskQueue taskQueue,
            IServiceProvider serviceProvider,
            ILogger<PlayerRebateBackground> logger)
        {
            _taskQueue = taskQueue;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"Queued Hosted Service is running. PlayerRebate background queue.");

            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var vipPlayerRebateFactory = scope.ServiceProvider.GetRequiredService<IPlayerRebateFactory>();
                    var cashServiceClient = scope.ServiceProvider.GetRequiredService<ICashServiceClient>();
                    var brandGameService = scope.ServiceProvider.GetRequiredService<ITgsService<BrandGame>>();
                    var brandService = scope.ServiceProvider.GetRequiredService<ITgsService<Brand>>();
                    var playerGameAggregateService = scope.ServiceProvider.GetRequiredService<ITgsService<PlayerGameRebateAggregate>>();
                    var playerRebateService = scope.ServiceProvider.GetRequiredService<ITgsService<PlayerRebate>>();
                    var playerService = scope.ServiceProvider.GetRequiredService<ITgsService<Player>>();

                    var request =
                        await _taskQueue.DequeueAsync(stoppingToken);

                    var previousDay = DateTimeOffset.Now.ToOffset(new(8, 0, 0)).AddDays(-1);
                    var reportDate = new DateTimeOffset(previousDay.Year, previousDay.Month, previousDay.Day, 0, 0, 0,
                        previousDay.Offset);

                    if (request.Date.IsNotNullOrEmpty() &&
                        DateTimeOffset.TryParse($"{request.Date} 00:00 +08:00", out var result))
                    {
                        reportDate = new(result.Year, result.Month, result.Day, 0, 0, 0,
                            result.Offset);
                    }

                    if (await playerRebateService.GetQueries()
                            .AnyAsync(x => x.ReportDate == reportDate.ToUniversalTime(), stoppingToken))
                    {
                        _logger.LogWarning($"The {request.BrandCode} rebates of {reportDate} have generated.");
                        
                        return;
                    }

                    var sportsGroup = Consts.SportsGroup.Select(x => (short)x).ToArray();
                    var slotGroup = Consts.SlotGroup.Select(x => (short)x).ToArray();

                    var playerRebates = await playerGameAggregateService
                        .GetQueries(q => q.Where(x => x.ReportDate == reportDate.ToUniversalTime()))
                        .GroupJoin(brandGameService.GetQueries(),
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
                        .ToArrayAsync(stoppingToken);

                    if (playerRebates.Length <= 0)
                    {
                        _logger.LogWarning($"The {request.BrandCode} ({reportDate}) Has no player rebate data.");

                        return;
                    }

                    var brandIds = playerRebates.Select(x => x.BrandId);
                    var brands = await brandService.GetQueries().Where(x => brandIds.Contains(x.Id))
                        .Select(x => new { x.Id, x.AutoSentPlayerBonus })
                        .ToArrayAsync(stoppingToken);

                    var autoSentPlayer = playerRebates
                        .Join(brands, p => p.BrandId, b => b.Id, (player, brand) => new { player, brand })
                        .Where(x => x.brand.AutoSentPlayerBonus).Select(x => x.player).ToArray();

                    var playerBonusesNotSend = playerRebates
                        .Join(brands, p => p.BrandId, b => b.Id, (player, brand) => new { player, brand })
                        .Where(x => !x.brand.AutoSentPlayerBonus).Select(x => x.player).ToArray();

                    var players = await playerService.GetQueries()
                        .Where(x => autoSentPlayer.Select(p => p.PlayerCode).Contains(x.PlayerCode))
                        .Select(x => new { x.BrandId, x.PlayerCode, x.Wallets, x.DefaultWalletId })
                        .ToArrayAsync(stoppingToken);

                    var rebates = await vipPlayerRebateFactory.CreateVipPlayerRebatesAsync(autoSentPlayer, DateTimeOffset.UtcNow).ConfigureAwait(false);

                    _ = await vipPlayerRebateFactory.CreateVipPlayerRebatesAsync(playerBonusesNotSend, null).ConfigureAwait(false);
                    await vipPlayerRebateFactory.CreatePlayerBucketsAsync(autoSentPlayer.Join(players, pb => pb.PlayerCode,
                            p => p.PlayerCode,
                            (pb, p) => new { pb, p }).Select(x =>
                            new CreatePlayerBucketRequest(x.pb.PlayerCode, BucketType.Vip, x.pb.RebateAmount)).ToArray())
                        .ConfigureAwait(false);

                    foreach (var playerBonus in rebates.Join(players, pb => pb.PlayerCode, p => p.PlayerCode,
                                 (pb, p) => new { pb, p }).Where(x => x.pb.RebateAmount > 0))
                    {
                        _ = await cashServiceClient.RebateAsync(
                            playerBonus.p.DefaultWalletId,
                            playerBonus.pb.RebateAmount,
                            null,
                            playerBonus.pb.Id.ToString()).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Player Rebate Error occurred executing task work item.");
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Player Rebate Queued Hosted Service is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}