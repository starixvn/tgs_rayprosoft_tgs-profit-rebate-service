using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.Core.ViewModel;

public class PlayerAllAgentTurnoverWinLossModel
{
	public Guid BrandId { get; set; }

	public string PlayerCode { get; set; } = default!;

    public string ChildPlayerCode { get; set; } = default!;

    public string PlayerUsername { get; set; } = default!;

    public GameType GameType { get; set; }

    public decimal Turnover { get; set; }

    public decimal WinLoss { get; set; }
}
