using System;
using System.Collections.Generic;
using Tgs.ProfitRebate.Repository.Enums;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class Player
    {
        public decimal AccumulatedDeposit { get; set; }

        public virtual Agent Agent { get; set; }

        public Guid? AgentId { get; set; }

        public string AreaCode { get; set; }

        public int BetLimitId { get; set; }

        public virtual Brand Brand { get; set; }

        public Guid BrandId { get; set; }

        public string DefaultWalletCurrencyCode { get; set; }

        public Guid DefaultWalletId { get; set; }

        public string Email { get; set; }

        public DateTimeOffset? FirstDepositOn { get; set; }

        public DateTimeOffset? FirstPromotionOn { get; set; }

        public bool IdentityAuthentication { get; set; }

        public string IdNumber { get; set; }

        public DateTimeOffset LastActiveOn { get; set; }

        public byte[] Password { get; set; }

        public string Phone { get; set; }

        public bool PhoneAuthentication { get; set; }

        public string PlayerCode { get; set; }

        public string QQ { get; set; }

        public string RealName { get; set; }

        public DateTimeOffset? RegisteredOn { get; set; }

        public byte[] Salt { get; set; }

        public PlayerStatus Status { get; set; }

        public PlayerType Type { get; set; }

        public string UpdatedByUsername { get; set; }

        public DateTimeOffset UpdatedOn { get; set; }

        public string Username { get; set; }

        public Guid? VipId { get; set; }

        public string Wechat { get; set; }

        public byte[] WithdrawPassword { get; set; }

        public byte[] WithdrawSalt { get; set; }
    }
}
