namespace Tgs.ProfitRebate.Core;

public class AgentDividendPercentageData
{
    public Guid AgentId { get; set; }

    public Guid? ParentAgentId { get; set; }

    public decimal DividendPercentage { get; set; }
}