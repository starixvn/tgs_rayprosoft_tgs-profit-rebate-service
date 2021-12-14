using System;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class AgentDividendCondition
    {
        public int Rank { get; set; }
        public Guid AgentId { get; set; }
        public virtual Agent Agent { get; set; }
        public decimal NetProfit { get; set; }
        public int ActiveNumber { get; set; }
        public decimal DividendPercentage { get; set; }
    }
}