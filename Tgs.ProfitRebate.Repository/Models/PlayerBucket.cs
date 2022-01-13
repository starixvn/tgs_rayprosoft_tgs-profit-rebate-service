using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class PlayerBucket
    {
        public Guid Id { get; set; }
        public string PlayerCode { get; set; }
        public virtual Player Player { get; set; }
        public decimal BucketAmount { get; set; }
        public decimal CurrentBucketAmount { get; set; }
        public BucketType BucketType { get; set; }
        public bool IsFull { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
    }
}