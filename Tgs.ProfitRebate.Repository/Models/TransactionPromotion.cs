using System;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class TransactionPromotion
    {
        public Guid TransactionId { get; set; }
        public Guid PromotionId { get; set; }
        public Guid PromotionBonusId { get; set; }
        public string PromotionType { get; set; }
        public virtual Transaction Transaction { get; set; }
        public virtual Promotion Promotion { get; set; }
    }
}