using System;
using System.Collections.Generic;
using Tgs.ProfitRebate.Repository.Enums;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class Promotion
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }
        public PromotionStatus Status { get; set; }
        public bool AutoSentPlayerBonus { get; set; }
        public Guid BrandId { get; set; }
        public virtual Brand Brand { get; set; }
        public string PromotionType { get; set; }
        public string SettlementIntervalType { get; set; }
        public decimal TotalAmountLimit { get; set; }
        public decimal AmountUsed { get; set; }
        public bool Unlimited { get; set; }
        public GameType? GameType { get; set; }
        public decimal Percentage { get; set; }
        public decimal BonusAmountLimit { get; set; }
        public decimal TurnoverMultiple { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset UpdatedOn { get; set; }
        public string CreatedByUsername { get; set; }
        public string UpdatedByUsername { get; set; }
        public string SubSubject { get; set; }
        public string Content { get; set; }
        public string Terms { get; set; }
        public string Localization { get; set; }
        public string DesktopBanner { get; set; }
        public string MobileBanner { get; set; }
    }
}