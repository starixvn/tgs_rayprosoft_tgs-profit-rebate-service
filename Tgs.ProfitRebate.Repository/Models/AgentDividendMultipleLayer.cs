namespace Tgs.ProfitRebate.Repository.Models
{
    public class AgentDividendMultipleLayer
    {
        public int Id { get; set; }
        public int AgentDividendId { get; set; }
        public virtual AgentDividend AgentDividend { get; set; }
        public Guid AgentId { get; set; }
        public Guid DividendProviderAgentId { get; set; }
        public string Layer { get; set; }
        public decimal Dividend { get; set; }
    }
}