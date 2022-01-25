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
        CreatePlayerAllAgentRebatesHandler : IRequestHandler<CreatePlayerAllAgentRebatesRequest,
            MessageResponse>
    {
        private readonly ICashServiceClient _cashServiceClient;
        private readonly ITgsService<PlayerAllAgentRebate> _playerAllAgentRebateService;
        private readonly IPlayerRebateFactory _playerRebateFactory;
        private readonly ITgsService<Player> _playerService;

        public CreatePlayerAllAgentRebatesHandler(
            ICashServiceClient cashServiceClient,
            ITgsService<Player> playerService,
            ITgsService<PlayerAllAgentRebate> playerAllAgentRebateService,
            IPlayerRebateFactory playerRebateFactory)
        {
            _cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
            _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
            _playerRebateFactory =
                playerRebateFactory ?? throw new ArgumentNullException(nameof(playerRebateFactory));
            _playerAllAgentRebateService = playerAllAgentRebateService ??
                throw new ArgumentNullException(nameof(playerAllAgentRebateService));
        }

        public async Task<MessageResponse> Handle(CreatePlayerAllAgentRebatesRequest request,
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

            if (await _playerAllAgentRebateService.GetQueries()
                    .AnyAsync(x => x.ReportDate == reportDate.ToUniversalTime(), cancellationToken))
            {
                return new MessageResponse
                {
                    Valid = false,
                    Message = $"The all agent rebates of {reportDate} have generated."
                };
            }

            var playerTurnoverWinLoss = await _playerRebateFactory.GetPlayerTurnoverWinLossAsync(reportDate);

            var brandIds = playerTurnoverWinLoss.Select(x => x.BrandId).Distinct().ToArray();

            var brandAllAgentGameTypeConditions =
                await _playerRebateFactory.GetBrandAllAgentGameTypeConditionsAsync(brandIds);

            var playerRebates = playerTurnoverWinLoss
                .Where(x => MatchConditionSetting(x, brandAllAgentGameTypeConditions))
                .SelectMany(x => GetRebates(x, brandAllAgentGameTypeConditions, reportDate)).ToArray();

            if (playerRebates.Length <= 0)
            {
                return new() { Valid = false, Message = "Has no player rebate data." };
            }

            var players = _playerService.GetQueries()
                .Where(x => playerRebates.Select(p => p.PlayerCode).Distinct().Contains(x.PlayerCode)).ToArray();

            var sentToPlayerRebates = playerRebates.Where(x => x.RebateAmount > 0)
                .Join(players, pb => pb.PlayerCode,
                    p => p.PlayerCode,
                    (pb, p) => new { pb, p }).Select(x => new
                {
                    x.pb.PlayerCode,
                    x.pb.RebateAmount,
                    x.p.DefaultWalletId
                }).GroupBy(x => new { x.PlayerCode, x.DefaultWalletId }, (group, list) => new
                {
                    group.PlayerCode,
                    group.DefaultWalletId,
                    RebateAmount = list.Sum(x => x.RebateAmount)
                }).ToArray();

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            await _playerRebateFactory.CreateAllAgentPlayerRebatesAsync(playerRebates, DateTimeOffset.UtcNow);
            await _playerRebateFactory.CreatePlayerBucketsAsync(sentToPlayerRebates.Select(x =>
                    new CreatePlayerBucketRequest(x.PlayerCode, BucketType.AllAgent, x.RebateAmount)).ToArray())
                .ConfigureAwait(false);

            scope.Complete();

            foreach (var playerBonus in sentToPlayerRebates)
            {
                var response = await _cashServiceClient.DividendAllAgentRebateAsync(
                    playerBonus.DefaultWalletId,
                    playerBonus.RebateAmount,
                    null,
                    null).ConfigureAwait(false);
            }

            return new MessageResponse
            {
                Valid = true
            };
        }

        private static bool MatchConditionSetting(PlayerAllAgentTurnoverWinLossModel model,
            IEnumerable<BrandAllAgentGameTypeConditionModel> conditions)
        {
            return conditions.Any(x => model.BrandId == x.BrandId &&
                ((short)model.GameType == (short)x.AllAgentGameType ||
                    model.GameType == GameType.Sportsbook && Consts.SportsGroup.Contains(model.GameType) ||
                    model.GameType == GameType.Slot && Consts.SlotGroup.Contains(model.GameType)));
        }

        private static IEnumerable<PlayerAllAgentRebateModel> GetRebates(PlayerAllAgentTurnoverWinLossModel model,
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
}