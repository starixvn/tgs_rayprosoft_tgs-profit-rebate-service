using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.Core.ViewModel;

public class PlayerAllAgentTurnoverWinLossPlayerNumberModel
{
    public Guid BrandId { get; set; }

    public string PlayerCode { get; set; } = default!;

    public AllAgentGameType AllAgentGameType { get; set; }

    public int PlayerNumber { get; set; }

    public decimal Turnover { get; set; }

    public decimal WinLoss { get; set; }
}