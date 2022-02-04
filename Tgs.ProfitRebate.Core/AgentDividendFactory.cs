using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Tgs.Core.Infrastructures.Utility;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository;
using Tgs.ProfitRebate.Repository.Models;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Core
{
    public class AgentDividendFactory : IAgentDividendFactory
    {
        private readonly ITgsService<AgentDividend> _agentDividendService;
        private readonly ITgsService<Agent> _agentService;
        private readonly IAgentWalletRepository _agentWalletRepository;
        private readonly ITgsService<Brand> _brandService;
        private readonly ITgsService<PlayerAggregate> _playerAggregateService;
        private readonly ITgsService<PlayerGameAggregate> _playerGameAggregateService;

        public AgentDividendFactory(
            ITgsService<Agent> agentService,
            ITgsService<Brand> brandService,
            ITgsService<AgentDividend> agentDividendService,
            ITgsService<PlayerAggregate> playerAggregateService,
            ITgsService<PlayerGameAggregate> playerGameAggregateService,
            IAgentWalletRepository agentWalletRepository)
        {
            _agentService = agentService;
            _brandService = brandService;
            _agentDividendService = agentDividendService;
            _playerAggregateService = playerAggregateService;
            _playerGameAggregateService = playerGameAggregateService;
            _agentWalletRepository = agentWalletRepository;
        }

        public (DateTimeOffset, DateTimeOffset) GetInterval(SettlementIntervalType type, DateTimeOffset startDate,
            int? dayOfMonth) => type switch
        {
            SettlementIntervalType.Daily => (startDate, startDate),
            SettlementIntervalType.Weekly => (DateGenerator.GetFirstDayOfWeek(startDate),
                DateGenerator.GetLastDayOfWeek(startDate)),
            SettlementIntervalType.Monthly => (startDate,
                dayOfMonth == null
                    ? DateGenerator.GetLastDayOfMonth(startDate)
                    : DateGenerator.GetSpecifyDayOfMonth(startDate, dayOfMonth.Value!)),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public async IAsyncEnumerable<Brand[]> GetSettlementIntervalBrands(string brandCode, DateTimeOffset queryDate)
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

        public async ValueTask SaveAgentDividendData(Brand[] brands, List<CreateAgentDividendModel> createModels,
            List<CreateAgentDividendModel> createAutoSendModels, List<AgentDividend> updateAgentDividends)
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            await CreateAgentDividendsAsync(createModels.ToArray(), null);

            await CreateAgentDividendsAsync(
                createAutoSendModels.ToArray(),
                DateTimeOffset.UtcNow);

            await _agentDividendService.UpdateEntitiesAsync(updateAgentDividends.ToArray());

            await _brandService.UpdateEntitiesAsync(brands);

            var createdAgentIds = createAutoSendModels.Select(x => x.AgentId).ToArray();

            var createdAgents = await _agentService.GetQueries().Where(x => createdAgentIds.Contains(x.Id))
                .ToArrayAsync();

            foreach (var models in createAutoSendModels)
            {
                var agent = createdAgents.Single(a => a.Id == models.AgentId);

                await _agentWalletRepository.ExchangeAsync(models.BrandId,
                    agent.DefaultWalletId,
                    agent.Id,
                    TransactionType.Dividend,
                    models.AgentDividendAmount);
            }

            scope.Complete();
        }

        public async Task<(List<CreateAgentDividendModel>, List<CreateAgentDividendModel>, List<AgentDividend>, int)>
            GetAgentDividends(Brand brand, int idNumber)
        {
            var agents = await _agentService.GetQueries()
                .Where(x => x.BrandId == brand.Id)
                .Include(x => x.AgentDividendConditions)
                .ToArrayAsync();

            var playerDataModels = await _playerAggregateService.GetQueries()
                .Where(x => x.AgentId != null)
                .Where(x => x.BrandId == brand.Id)
                .Where(x => x.ReportDate >= brand.CurrentSettlementFrom &&
                    x.ReportDate <= brand.CurrentSettlementTo)
                .GroupBy(x => new
                    {
                        x.BrandId,
                        x.AgentId,
                        x.PlayerCode
                    },
                    (groupItem, playerAggregates) => new
                    {
                        groupItem.AgentId,
                        groupItem.PlayerCode,
                        groupItem.BrandId,
                        PromotionAmount = playerAggregates.Sum(p => p.PromotionAmount),
                        RebateAmount = playerAggregates.Sum(p => p.RebateAmount),
                        DepositAmount = playerAggregates.Sum(p => p.DepositAmount),
                        WithdrawalAmount = playerAggregates.Sum(p => p.WithdrawalAmount),
                        AdjustmentAmount = playerAggregates.Sum(p => p.AdjustmentAmount),
                        BonusAmount = playerAggregates.Sum(p => p.RebateAmount + p.PromotionAmount),
                        BetAmount = playerAggregates.Sum(p => p.BetAmount),
                        ValidBetAmount = playerAggregates.Sum(p => p.ValidBetAmount),
                        OriginalTurnover = playerAggregates.Sum(p => p.OriginalTurnover),
                        Turnover = playerAggregates.Sum(p => p.Turnover),
                        WinLossAmount = playerAggregates.Sum(p => p.WinLossAmount)
                    }).ToArrayAsync();

            var playerData = playerDataModels.Select(x => new PlayerGroupDataModel
            {
                AgentId = x.AgentId.Value,
                PlayerCode = x.PlayerCode,
                BrandId = x.BrandId,
                PromotionAmount = x.PromotionAmount,
                RebateAmount = x.RebateAmount,
                DepositAmount = x.DepositAmount,
                WithdrawalAmount = x.WithdrawalAmount,
                AdjustmentAmount = x.AdjustmentAmount,
                BonusAmount = x.BonusAmount,
                ValidBetAmount = x.ValidBetAmount,
                OriginalTurnover = x.OriginalTurnover,
                BetAmount = x.BetAmount,
                Turnover = x.Turnover,
                WinLossAmount = x.WinLossAmount,
                NetProfit = x.WinLossAmount - x.AdjustmentAmount - x.RebateAmount - x.PromotionAmount -
                    x.WinLossAmount * brand.PlatformCharge / 100
            }).ToArray();

            var agentCurrentConditions = playerData.GroupBy(x => x.AgentId, (agentId, items) => new AgentConditionModel
            {
                AgentId = agentId,
                ActiveCount = items.Distinct().Count(),
                NetProfit = items.Sum(s => s.NetProfit)
            }).ToArray();

            var parentAgentDividends = playerData.SelectMany(x
                => brand.AgentDividendType == AgentDividendType.MultipleLayer
                    ? GetAgentDividendsByMultipleLayer(agents, agentCurrentConditions,
                        x.AgentId,
                        x.NetProfit,
                        brand.CheckDividendActiveCount)
                    : GetAgentDividends(agents,
                        x.AgentId,
                        x.NetProfit)).ToArray();

            var agentData = playerData.GroupBy(x => new
                {
                    x.BrandId,
                    x.AgentId
                },
                (groupItem, playerAggregates) => new
                {
                    groupItem.AgentId,
                    groupItem.BrandId,
                    PromotionAmount = playerAggregates.Sum(p => p.PromotionAmount),
                    RebateAmount = playerAggregates.Sum(p => p.RebateAmount),
                    DepositAmount = playerAggregates.Sum(p => p.DepositAmount),
                    WithdrawalAmount = playerAggregates.Sum(p => p.WithdrawalAmount),
                    AdjustmentAmount = playerAggregates.Sum(p => p.AdjustmentAmount),
                    BonusAmount = playerAggregates.Sum(p => p.BonusAmount),
                    BetAmount = playerAggregates.Sum(p => p.BetAmount),
                    ValidBetAmount = playerAggregates.Sum(p => p.ValidBetAmount),
                    OriginalTurnover = playerAggregates.Sum(p => p.OriginalTurnover),
                    Turnover = playerAggregates.Sum(p => p.Turnover),
                    WinLossAmount = playerAggregates.Sum(p => p.WinLossAmount),
                    NetProfit = playerAggregates.Sum(p => p.NetProfit)
                });

            var allAgents = agents.GroupJoin(agentData,
                agent => new
                {
                    agent.Id,
                    agent.BrandId
                },
                data => new
                {
                    Id = data.AgentId,
                    data.BrandId
                },
                (agent, data) => new
                {
                    agent,
                    data
                }).SelectMany(x => x.data.DefaultIfEmpty(),
                (x, data) => new
                {
                    AgentId = x.agent.Id,
                    x.agent.Username,
                    x.agent.BrandId,
                    DividendPercentage =
                        x.agent.AgentDividendConditions.FirstOrDefault(c => c.Rank == 1)?.DividendPercentage ?? 0M,
                    data?.PromotionAmount,
                    data?.RebateAmount,
                    data?.DepositAmount,
                    data?.WithdrawalAmount,
                    data?.AdjustmentAmount,
                    data?.BonusAmount,
                    data?.BetAmount,
                    data?.ValidBetAmount,
                    data?.OriginalTurnover,
                    data?.Turnover,
                    data?.WinLossAmount,
                    data?.NetProfit
                });

            var agentGameData = _playerGameAggregateService.GetQueries()
                .Where(x => x.AgentId != null)
                .Where(x => x.BrandId == brand.Id)
                .Where(x => x.ReportDate >= brand.CurrentSettlementFrom &&
                    x.ReportDate <= brand.CurrentSettlementTo).GroupBy(x => new
                    {
                        x.BrandId,
                        x.AgentId
                    },
                    (agent, playerGames) => new
                    {
                        AgentId = agent.AgentId.Value,
                        agent.BrandId,
                        LiveDealerWinLoss =
                            playerGames
                                .Where(l => l.GameType == GameType.LiveDealer)
                                .Sum(l => l.WinLoss),
                        SlotWinLoss =
                            playerGames
                                .Where(l => l.GameType == GameType.Slot)
                                .Sum(l => l.WinLoss),
                        FishingWinLoss =
                            playerGames
                                .Where(l => l.GameType == GameType.Fishing)
                                .Sum(l => l.WinLoss),
                        ArcadeWinLoss =
                            playerGames
                                .Where(l => l.GameType == GameType.Arcade)
                                .Sum(l => l.WinLoss),
                        TableGameWinLoss =
                            playerGames
                                .Where(l => l.GameType == GameType.TableGame)
                                .Sum(l => l.WinLoss),
                        LotteryWinLoss =
                            playerGames
                                .Where(l => l.GameType == GameType.Lottery)
                                .Sum(l => l.WinLoss),
                        CardGameWinLoss =
                            playerGames
                                .Where(l => l.GameType == GameType.CardGame)
                                .Sum(l => l.WinLoss),
                        SportsWinLoss =
                            playerGames
                                .Where(l => l.GameType == GameType.Sportsbook)
                                .Sum(l => l.WinLoss),
                        ESportsWinLoss =
                            playerGames
                                .Where(l => l.GameType == GameType.ESports)
                                .Sum(l => l.WinLoss),
                        VirtualSportsWinLoss =
                            playerGames
                                .Where(l => l.GameType == GameType.VirtualSports)
                                .Sum(l => l.WinLoss),
                        WinLoss = playerGames
                            .Sum(l => l.WinLoss)
                    });

            var agentDividends = allAgents.GroupJoin(agentGameData,
                    agent => new
                    {
                        agent.BrandId,
                        agent.AgentId
                    },
                    agentGame => new
                    {
                        agentGame.BrandId,
                        agentGame.AgentId
                    },
                    (agent, games) => new
                    {
                        agent,
                        games
                    })
                .SelectMany(x => x.games.DefaultIfEmpty(),
                    (x, games) => new CreateAgentDividendModel
                    {
                        AgentUsername = x.agent.Username,
                        AgentId = x.agent.AgentId,
                        BrandId = x.agent.BrandId,
                        AutoSend = brand.AutoSentAgentBonus,
                        From = brand.CurrentSettlementFrom,
                        To = brand.CurrentSettlementTo,
                        PromotionAmount = x.agent?.PromotionAmount ?? 0M,
                        RebateAmount = x.agent?.RebateAmount ?? 0M,
                        DepositAmount = x.agent?.DepositAmount ?? 0M,
                        WithdrawalAmount = x.agent?.WithdrawalAmount ?? 0M,
                        AdjustmentAmount = x.agent?.AdjustmentAmount ?? 0M,
                        BonusAmount = x.agent?.BonusAmount ?? 0M,
                        BetAmount = x.agent?.BetAmount ?? 0M,
                        ValidBetAmount = x.agent?.ValidBetAmount ?? 0M,
                        OriginalTurnover = x.agent?.OriginalTurnover ?? 0M,
                        Turnover = x.agent?.Turnover ?? 0M,
                        WinLossAmount = x.agent?.WinLossAmount ?? 0M,
                        NetProfit = x.agent?.NetProfit ?? 0M,
                        LiveDealerWinLoss = games?.LiveDealerWinLoss ?? 0M,
                        SlotWinLoss = games?.SlotWinLoss ?? 0M,
                        FishingWinLoss = games?.FishingWinLoss ?? 0M,
                        ArcadeWinLoss = games?.ArcadeWinLoss ?? 0M,
                        TableGameWinLoss = games?.TableGameWinLoss ?? 0M,
                        LotteryWinLoss = games?.LotteryWinLoss ?? 0M,
                        CardGameWinLoss = games?.CardGameWinLoss ?? 0M,
                        SportsWinLoss = games?.SportsWinLoss ?? 0M,
                        ESportsWinLoss = games?.ESportsWinLoss ?? 0M,
                        VirtualSportsWinLoss = games?.VirtualSportsWinLoss ?? 0M,
                        AgentDividendPercentage = x.agent?.DividendPercentage ?? 0M,
                        AgentDividendAmount =
                            parentAgentDividends.Where(p => p.AgentId == x.agent.AgentId).Sum(s => s.Dividend),
                        AgentDividendMultipleLayerModels =
                            parentAgentDividends.Where(p => p.AgentId == x.agent.AgentId).ToArray(),
                        CreatedOn = DateTimeOffset.UtcNow
                    }).Where(x => x.DepositAmount != 0M || x.WithdrawalAmount != 0M || x.AdjustmentAmount != 0M ||
                    x.BonusAmount != 0M || x.BetAmount != 0M || x.Turnover != 0M || x.WinLossAmount != 0M ||
                    x.NetProfit != 0M || x.AgentDividendPercentage != 0M || x.AgentDividendAmount != 0M ||
                    x.PromotionAmount != 0M || x.RebateAmount != 0M || x.OriginalTurnover != 0M ||
                    x.ValidBetAmount != 0M).ToArray();

            var agentIds = agentDividends.Where(x => x.AgentDividendAmount >= 0M).Select(x => x.AgentId).ToArray();

            var noSendDividends = await _agentDividendService.GetQueries()
                .Where(x => agentIds.Contains(x.AgentId) && x.SentOn == null).ToArrayAsync()
                .ConfigureAwait(false);

            foreach (var dividend in agentDividends)
            {
                dividend.Id = idNumber += 1;

                var agentNoSendDividends = noSendDividends.Where(x => x.AgentId == dividend.AgentId).ToArray();

                var agentNoSendDividend = agentNoSendDividends.Sum(x => x.AgentDividendAmount);

                if (agentNoSendDividend < 0)
                {
                    if (dividend.AgentDividendAmount + agentNoSendDividend > 0M)
                    {
                        dividend.BeforeDividendAmount = dividend.AgentDividendAmount;
                        dividend.AgentDividendAmount += agentNoSendDividend;

                        foreach (var negativeDividend in agentNoSendDividends)
                        {
                            negativeDividend.ReferencedAgentDividendId = dividend.Id;
                            negativeDividend.SentOn = DateTimeOffset.UtcNow;
                        }
                    }
                    else
                    {
                        dividend.AutoSend = false;
                    }
                }
            }

            return (agentDividends.Where(x =>
                    x.AgentDividendAmount >= 0M && x.AutoSend).ToList(),
                agentDividends.Where(x =>
                    x.AgentDividendAmount < 0M || !x.AutoSend).ToList(),
                noSendDividends.Where(x => x.ReferencedAgentDividendId is not null).ToList(),
                agentDividends.Max(x => x.Id) ?? idNumber);
        }

        private ValueTask CreateAgentDividendsAsync(CreateAgentDividendModel[] models, DateTimeOffset? sentOn) =>
            _agentDividendService.CreateEntitiesAsync(models.Select(model => new AgentDividend
            {
                Id = model.Id.Value,
                BrandId = model.BrandId,
                AgentId = model.AgentId,
                From = model.From,
                To = model.To,
                AgentDividendPercentage = model.AgentDividendPercentage,
                PromotionAmount = model.PromotionAmount,
                RebateAmount = model.RebateAmount,
                DepositAmount = model.DepositAmount,
                WithdrawalAmount = model.WithdrawalAmount,
                AdjustmentAmount = model.AdjustmentAmount,
                BonusAmount = model.BonusAmount,
                BetAmount = model.BetAmount,
                ValidBetAmount = model.ValidBetAmount,
                OriginalTurnover = model.OriginalTurnover,
                Turnover = model.Turnover,
                WinLossAmount = model.WinLossAmount,
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
                CreatedOn = model.CreatedOn,
                SentOn = sentOn,
                BeforeDividendAmount = model.BeforeDividendAmount,
                ReferencedAgentDividendId = model.ReferencedAgentDividendId,
                AgentDividendMultipleLayers = model.AgentDividendMultipleLayerModels.Select(x
                    => new AgentDividendMultipleLayer
                    {
                        AgentId = x.AgentId,
                        AgentDividendId = model.Id.Value,
                        DividendProviderAgentId = x.DividendProviderAgentId,
                        Layer = x.Layer,
                        Dividend = x.Dividend
                    }).ToArray()
            }).ToArray());

        private static decimal GetAgentDividendPercentage(AgentDividendCondition[] agentDividendCondition,
            AgentConditionModel currentCondition, bool checkDividendActiveCount)
            => agentDividendCondition.OrderByDescending(x => x.Rank).Where(!checkDividendActiveCount,
                    x => currentCondition.NetProfit >= x.NetProfit)
                .Where(checkDividendActiveCount, x => currentCondition.ActiveCount >= x.ActiveNumber &&
                    currentCondition.NetProfit >= x.NetProfit).Select(x => x.DividendPercentage).FirstOrDefault();

        private static IEnumerable<AgentDividendMultipleLayerModel> GetAgentDividendsByMultipleLayer(
            Agent[] agents, AgentConditionModel[] agentCurrentConditions, Guid agentId,
            decimal netProfit, bool checkDividendActiveCount)
        {
            var agent = agents.First(x => x.Id == agentId);
            var currentAgentId = agentId;
            var providerAgentId = agent.Id;
            var layer = agent.Id.ToString();

            var agentDividendPercentage = GetAgentDividendPercentage(agent.AgentDividendConditions.ToArray(),
                agentCurrentConditions.First(x => x.AgentId == currentAgentId), checkDividendActiveCount);

            yield return new AgentDividendMultipleLayerModel
            {
                AgentId = currentAgentId,
                DividendProviderAgentId = providerAgentId,
                Layer = layer,
                Dividend = netProfit * agentDividendPercentage / 100M
            };
        }

        private static IEnumerable<AgentDividendMultipleLayerModel> GetAgentDividends(
            Agent[] agents, Guid agentId,
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

                yield return new AgentDividendMultipleLayerModel
                {
                    AgentId = currentAgentId,
                    DividendProviderAgentId = providerAgentId,
                    Layer = layer,
                    Dividend = netProfit * (agentDividendPercentage -
                        childDividendPercentage) / 100M
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
    }
}