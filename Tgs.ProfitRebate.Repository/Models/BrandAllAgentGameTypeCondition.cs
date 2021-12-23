using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class BrandAllAgentGameTypeCondition
    {
        public AllAgentGameType AllAgentGameType { get; set; }

        public AllAgentRebateType AllAgentRebateType { get; set; }

        public int Rank { get; set; }

        public Guid BrandId { get; set; }

        public decimal NetProfit { get; set; }

        public int ActiveNumber { get; set; }

        public decimal RebatePercentage { get; set; }
    }
}