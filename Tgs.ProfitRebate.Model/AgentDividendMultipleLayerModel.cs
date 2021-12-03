namespace Tgs.ProfitRebate.Model
{
    public class AgentDividendMultipleLayerModel
    {
        public Guid AgentId { get; set; }
        public Guid DividendProviderAgentId { get; set; }
        public string Layer { get; set; }
        public decimal Dividend { get; set; }
    }
}