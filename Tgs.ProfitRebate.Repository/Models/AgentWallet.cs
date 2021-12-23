namespace Tgs.ProfitRebate.Repository.Models
{
    public class AgentWallet
    {
        public Guid Id { get; set; }
        public Guid AgentId { get; set; }
        public virtual Agent Agent { get; set; }
        public string CurrencyCode { get; set; }
        public virtual Currency Currency { get; set; }
        public decimal Balance { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset UpdatedOn { get; set; }
    }
}