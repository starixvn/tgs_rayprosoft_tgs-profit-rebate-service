using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class BrandAllAgentGameType
    {
        public Guid BrandId { get; set; }

        public AllAgentGameType AllAgentGameType { get; set; }

        public bool Switch { get; set; }

        public bool HasValidBet { get; set; }

        public bool HasWinLoss { get; set; }
    }
}