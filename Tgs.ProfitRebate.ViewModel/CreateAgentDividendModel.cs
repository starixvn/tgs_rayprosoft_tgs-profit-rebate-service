using System;

namespace Tgs.ProfitRebate.ViewModel
{
    public class CreateAgentDividendModel
    {
        public int? Id { get; set; }

        public int? ReferencedAgentDividendId { get; set; }

        public string AgentUsername { get; set; }

        public Guid AgentId { get; set; }

        public Guid BrandId { get; set; }

        public DateTimeOffset From { get; set; }

        public DateTimeOffset To { get; set; }

        public decimal PromotionAmount { get; set; }

        public decimal RebateAmount { get; set; }

        public decimal DepositAmount { get; set; }

        public decimal WithdrawalAmount { get; set; }

        public decimal AdjustmentAmount { get; set; }

        public decimal BonusAmount { get; set; }

        public decimal BetAmount { get; set; }

        public decimal ValidBetAmount { get; set; }

        public decimal OriginalTurnover { get; set; }

        public decimal Turnover { get; set; }

        public decimal WinLossAmount { get; set; }

        public decimal NetProfit { get; set; }

        public decimal LiveDealerWinLoss { get; set; }

        public decimal SlotWinLoss { get; set; }

        public decimal FishingWinLoss { get; set; }

        public decimal ArcadeWinLoss { get; set; }

        public decimal TableGameWinLoss { get; set; }

        public decimal LotteryWinLoss { get; set; }

        public decimal CardGameWinLoss { get; set; }

        public decimal SportsWinLoss { get; set; }

        public decimal ESportsWinLoss { get; set; }

        public decimal VirtualSportsWinLoss { get; set; }

        public decimal AgentDividendPercentage { get; set; }

        public decimal AgentDividendAmount { get; set; }

        public decimal? BeforeDividendAmount { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public bool AutoSend { get; set; }

        public AgentDividendMultipleLayerModel[] AgentDividendMultipleLayerModels { get; set; }
    }
}