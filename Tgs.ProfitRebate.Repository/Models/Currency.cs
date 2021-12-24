namespace Tgs.ProfitRebate.Repository.Models
{
    public class Currency
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public string NativeName { get; set; }

        public string Symbol { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset UpdatedOn { get; set; }

        public string CreatedByUsername { get; set; }

        public string UpdatedByUsername { get; set; }

        public virtual ICollection<Wallet> Wallets { get; set; }
    }
}