using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class PlayerAllAgentRebate
    {
        public int Id { get; set; }
        public Guid BrandId { get; set; }
        public string PlayerCode { get; set; }
        public DateTimeOffset ReportDate { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset? SentOn { get; set; }
        public AllAgentGameType AllAgentGameType { get; set; }
        public GameType GameType { get; set; }
        public AllAgentRebateType AllAgentRebateType { get; set; }
        public int Rank { get; set; }
        public int ActiveNumber { get; set; }
        public decimal ConditionAmount { get; set; }
        public decimal RebatePercentage { get; set; }
        public int ActualPlayerNumber { get; set; }
        public decimal ActualConditionAmount { get; set; }
        public decimal RebateAmount { get; set; }
    }
}