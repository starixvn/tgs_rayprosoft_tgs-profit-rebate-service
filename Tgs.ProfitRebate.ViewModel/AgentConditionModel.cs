using System;

namespace Tgs.ProfitRebate.ViewModel
{
	public class AgentConditionModel
	{
		public Guid AgentId { get; set; }

		public decimal ActiveCount { get; set; }

		public decimal NetProfit { get; set; }

		public decimal DividendPercentage { get; set; }
	}
}
