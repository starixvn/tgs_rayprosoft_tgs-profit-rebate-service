using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.Core.ViewModel;

public class PlayerAllAgentRebateModel
{
    public Guid BrandId { get; set; }

    public string PlayerCode { get; set; } = default!;

    public DateTimeOffset ReportDate { get; set; }

    public AllAgentGameType AllAgentGameType { get; set; }

    public AllAgentRebateType AllAgentRebateType { get; set; }

    public int Rank { get; set; }

    public int ActiveNumber { get; set; }

    public decimal ConditionAmount { get; set; }

    public decimal RebatePercentage { get; set; }

    public int ActualPlayerNumber { get; set; }

    public decimal ActualConditionAmount { get; set; }

    public decimal RebateAmount { get; set; }
}