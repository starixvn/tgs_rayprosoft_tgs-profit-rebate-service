namespace Tgs.ProfitRebate.Repository.Models
{
    public class PlayerAggregate
    {
        public int Id { get; set; }

        public Guid BrandId { get; set; }

        public Guid? AgentId { get; set; }

        public string PlayerCode { get; set; }

        public int Hour { get; set; }

        public int Minute { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset ReportDate { get; set; }

        public int RegisteredCount { get; set; }

        public int FirstFundInCount { get; set; }

        public decimal DepositAmount { get; set; }

        public decimal WithdrawalAmount { get; set; }

        public decimal CompanyAdjustAmount { get; set; }

        public decimal BonusAmount { get; set; }

        public decimal RebateAmount { get; set; }

        public decimal DividendAmount { get; set; }

        public decimal PromotionAmount { get; set; }

        public decimal BetAmount { get; set; }

        public decimal ValidBetAmount { get; set; }

        public decimal OriginalTurnover { get; set; }

        public decimal Turnover { get; set; }

        public decimal WinLossAmount { get; set; }

        public decimal AgentAdjustAmount { get; set; }

        public decimal SystemAdjustAmount { get; set; }
    }
}