using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.Core.ViewModel;

public class PlayerAllAgentTurnoverWinLossModel
{
	public Guid BrandId { get; set; }

	public string ParentPlayerCode { get; set; } = default!;

    public string PlayerCode { get; set; } = default!;

    public string PlayerUsername { get; set; } = default!;

    public GameType GameType { get; set; }

	public int PlayerNumber { get; set; }

	public decimal SportsTurnover { get; set; }

	public decimal VirtualSportsTurnover { get; set; }

	public decimal LiveDealerTurnover { get; set; }

	public decimal CardGameTurnover { get; set; }

	public decimal SlotTurnover { get; set; }

	public decimal LotteryTurnover { get; set; }

	public decimal SportsWinLoss { get; set; }

	public decimal VirtualSportsWinLoss { get; set; }

	public decimal LiveDealerWinLoss { get; set; }

	public decimal CardGameWinLoss { get; set; }

	public decimal SlotWinLoss { get; set; }

	public decimal LotteryWinLoss { get; set; }
}
