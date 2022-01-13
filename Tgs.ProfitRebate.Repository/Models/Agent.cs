using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class Agent
    {
        public AgentConfigStatus AgentStatus { get; set; }

        public virtual Brand Brand { get; set; }

        public Guid BrandId { get; set; }

        public decimal CardGameRebatePercentage { get; set; }

        public decimal CommissionPercentage { get; set; }

        public string CreatedByUsername { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public Guid DefaultWalletId { get; set; }

        //佔成
        public decimal DividendPercentage { get; set; }

        public string? Email { get; set; }

        public Guid Id { get; set; }

        public string InvitationCode { get; set; }

        public AgentLevelType LevelType { get; set; }

        public decimal LiveDealerRebatePercentage { get; set; }

        public decimal LotteryRebatePercentage { get; set; }

        public virtual Agent ParentAgent { get; set; }

        public Guid? ParentAgentId { get; set; }

        public byte[] Password { get; set; }

        public string? Phone { get; set; }

        public virtual ICollection<Player> Players { get; set; }

        public string? QQ { get; set; }

        public string? RealName { get; set; }

        public DateTimeOffset RegisteredOn { get; set; }

        public string? Remarks { get; set; }

        public byte[] Salt { get; set; }

        public decimal SlotRebatePercentage { get; set; }

        public decimal SportsRebatePercentage { get; set; }

        public AgentStatus Status { get; set; }

        public virtual ICollection<Agent> SubAgents { get; set; }

        public AgentType Type { get; set; }

        public string UpdatedByUsername { get; set; }

        public DateTimeOffset UpdatedOn { get; set; }

        public string Username { get; set; }

        public decimal VirtualSportsRebatePercentage { get; set; }

        public string? Wechat { get; set; }

        public byte[] WithdrawPassword { get; set; }

        public byte[] WithdrawSalt { get; set; }
        public virtual ICollection<AgentWallet> Wallets { get; set; }
        public virtual ICollection<AgentDividendCondition> AgentDividendConditions { get; set; }
    }
}