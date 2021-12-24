using System;
using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.ViewModel
{
    public class BrandAllAgentGameTypeConditionModel
    {
        public Guid BrandId { get; set; }
        public bool GameTypeSwitch { get; set; }
        public bool HasValidBet { get; set; }
        public bool HasWinLoss { get; set; }
        public AllAgentGameType AllAgentGameType { get; set; }
        public int Rank { get; set; }
        public int ActiveNumber { get; set; }
        public decimal NetProfit { get; set; }
        public decimal RebatePercentage { get; set; }
    }
}