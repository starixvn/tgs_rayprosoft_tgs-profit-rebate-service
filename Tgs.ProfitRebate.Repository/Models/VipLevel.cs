namespace Tgs.ProfitRebate.Repository.Models
{
    public class VipLevel
    {
        public Guid Id { get; set; }
        public int Level { get; set; }
        public Guid BrandId { get; set; }
        public virtual Brand Brand { get; set; }
        public decimal LevelUpAccumulateBetAmount { get; set; }
        public decimal LevelUpAccumulateDeposit { get; set; }
        public decimal LevelKeepAccumulateBetAmount { get; set; }
        public decimal LevelKeepAccumulateDeposit { get; set; }
        public bool LevelKeepCondition { get; set; }
        public decimal BirthReward { get; set; }
        public decimal PromotionReward { get; set; }
        public decimal QuarterlyReward { get; set; }
        public decimal LiveDealerRebatePercentage { get; set; }
        public decimal SlotRebatePercentage { get; set; }
        public decimal LotteryRebatePercentage { get; set; }
        public decimal CardGameRebatePercentage { get; set; }
        public decimal SportsRebatePercentage { get; set; }
        public decimal VirtualSportsRebatePercentage { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset UpdatedOn { get; set; }
        public string CreatedByUsername { get; set; }
        public string UpdatedByUsername { get; set; }
        public virtual ICollection<Player> Players { get; set; }
        public virtual ICollection<Promotion> Promotions { get; set; }
    }
}