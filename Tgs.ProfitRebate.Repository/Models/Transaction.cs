using System;
using Tgs.ProfitRebate.Repository.Enums;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public string PlayerCode { get; set; }
        public Guid WalletId { get; set; }
        public virtual Wallet Wallet { get; set; }
        public Guid BrandId { get; set; }
        public virtual Brand Brand { get; set; }
        public string ExternalTransactionId { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public string Description { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public TransactionType Type { get; set; }
        public DateTimeOffset RecordedOn { get; set; }
        public DateTimeOffset? SentOn { get; set; }
    }
}