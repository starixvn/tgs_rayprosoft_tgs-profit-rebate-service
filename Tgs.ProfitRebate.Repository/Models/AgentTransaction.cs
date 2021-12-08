using System;
using Tgs.ProfitRebate.Repository.Enums;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class AgentTransaction
    {
        public Guid Id { get; set; }
        public Guid AgentId { get; set; }
        public Guid WalletId { get; set; }
        public virtual AgentWallet Wallet { get; set; }
        public Guid BrandId { get; set; }
        public virtual Brand Brand { get; set; }
        public string ExternalTransactionId { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public string Description { get; set; }
        public TransactionType Type { get; set; }
        public DateTimeOffset RecordedOn { get; set; }
        public DateTimeOffset? SentOn { get; set; }
    }
}