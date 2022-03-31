using System;

namespace Tgs.ProfitRebate.ViewModel
{
    public class PlayerGroupDataModel
    {
        public Guid AgentId { get; set; }
        public string PlayerCode { get; set; } = default!;
        public Guid BrandId { get; set; }
        public decimal PromotionAmount { get; set; }
        public decimal RebateAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal WithdrawalAmount { get; set; }
        public decimal CompanyAdjustAmount { get; set; }
        public decimal AgentAdjustAmount { get; set; }
        public decimal BonusAmount { get; set; }
        public decimal BetAmount { get; set; }
        public decimal ValidBetAmount { get; set; }
        public decimal OriginalTurnover { get; set; }
        public decimal Turnover { get; set; }
        public decimal WinLossAmount { get; set; }
        public decimal NoIncludeNetProfitCompanyAdjustAmount { get; set; }
        public decimal NoIncludeNetProfitAgentAdjustAmount { get; set; }
        public decimal NetProfit { get; set; }
    }
}