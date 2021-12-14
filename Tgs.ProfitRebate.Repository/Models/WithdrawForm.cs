using System;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class WithdrawForm
    {
        public decimal? ActualAmount { get; set; }

        public decimal Amount { get; set; }

        public string ApplyInfo { get; set; }

        public string BankInfo { get; set; }

        public Guid BankInfoId { get; set; }

        public virtual Brand Brand { get; set; }

        public Guid BrandId { get; set; }

        public Guid? BrandPaymentId { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public Guid Id { get; set; }

        public DateTimeOffset? PaidOn { get; set; }

        public string PaidUserId { get; set; }

        public string PaidUserName { get; set; }

        public string PaymentReturn { get; set; }

        public string Remark { get; set; }

        public DateTimeOffset? ReviewedOn { get; set; }

        public string ReviewedUserId { get; set; }

        public string ReviewedUserName { get; set; }

        public int Status { get; set; }

        public string ThirdPartyOrderNo { get; set; }

        public DateTimeOffset UpdatedOn { get; set; }

        public string UserId { get; set; }

        public string UserName { get; set; }

        public int UserType { get; set; }

        public string OrderNo { get; set; }
    }
}
