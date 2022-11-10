using System;

namespace Tgs.ProfitRebate.ViewModel;

public class VipPlayerRebateModel
{
	public int Id { get; set; }

	public Guid BrandId { get; set; }

	public Guid? AgentId { get; set; }

	public int GameId { get; set; }

	public string PlayerCode { get; set; } = default!;

	public DateTimeOffset CreatedOn { get; set; }

	public DateTimeOffset ReportDate { get; set; }

	public decimal SportsTurnover { get; set; }

	public decimal SportsRebateAmount { get; set; }

	public decimal VirtualSportsTurnover { get; set; }

	public decimal VirtualSportsRebateAmount { get; set; }

	public decimal LiveDealerTurnover { get; set; }

	public decimal LiveDealerRebateAmount { get; set; }

	public decimal CardGameTurnover { get; set; }

	public decimal CardGameRebateAmount { get; set; }

	public decimal SlotTurnover { get; set; }

	public decimal SlotRebateAmount { get; set; }

	public decimal LotteryTurnover { get; set; }

	public decimal LotteryRebateAmount { get; set; }

	public decimal VipLiveDealerRebatePercentage { get; set; }

	public decimal VipSlotRebatePercentage { get; set; }

	public decimal VipCardGameRebatePercentage { get; set; }

	public decimal VipSportsRebatePercentage { get; set; }

	public decimal VipVirtualSportsRebatePercentage { get; set; }

	public decimal VipLotteryRebatePercentage { get; set; }

	public decimal RebateAmount { get; set; }
}
