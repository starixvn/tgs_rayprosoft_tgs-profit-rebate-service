using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class PlayerGameAggregate
    {
        public int Id { get; set; }

        public Guid BrandId { get; set; }

        public Guid? AgentId { get; set; }

        public Guid GameProviderId { get; set; }

        public int GameId { get; set; }

        public string PlayerCode { get; set; }

        public int? VipLevel { get; set; }

        public GameType GameType { get; set; }

        public int Hour { get; set; }

        public int Minute { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset ReportDate { get; set; }

        public decimal BetAmount { get; set; }

        public decimal ValidBetAmount { get; set; }

        public decimal OriginalTurnover { get; set; }

        public decimal Turnover { get; set; }

        public decimal WinLoss { get; set; }

        public decimal VipLiveDealerRebatePercentage { get; set; }

        public decimal VipSlotRebatePercentage { get; set; }

        public decimal VipCardGameRebatePercentage { get; set; }

        public decimal VipSportsRebatePercentage { get; set; }

        public decimal VipVirtualSportsRebatePercentage { get; set; }

        public decimal VipLotteryRebatePercentage { get; set; }

        public decimal VipRebateAmount { get; set; }
    }
}