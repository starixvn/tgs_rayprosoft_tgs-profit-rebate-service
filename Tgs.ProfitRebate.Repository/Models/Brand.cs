using System;
using System.Collections.Generic;
using Tgs.ProfitRebate.Repository.Enums;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class Brand : BaseEntity<Guid>
    {

        public string ApiKey { get; set; }

        public bool AutoSentAgentBonus { get; set; }

        public bool AutoSentPlayerBonus { get; set; }

        public bool CheckDividendActiveCount { get; set; }

        public string ClientId { get; set; }

        public DateTimeOffset CurrentSettlementFrom { get; set; }

        public DateTimeOffset CurrentSettlementTo { get; set; }

        public string? Description { get; set; }

        public bool EnableSms { get; set; }

        public bool EnableSmsRegisterFlow { get; set; }

        public virtual Licensee Licensee { get; set; }

        public Guid LicenseeId { get; set; }

        public string? LogoInfo { get; set; }

        public DateTimeOffset NextSettlementFrom { get; set; }

        public DateTimeOffset NextSettlementTo { get; set; }

        public string? OperationalEmail { get; set; }

        public decimal PlatformCharge { get; set; }

        public virtual ICollection<Player> Players { get; set; }

        public DateTimeOffset PreviousSettlementFrom { get; set; }

        public DateTimeOffset PreviousSettlementTo { get; set; }

        public string SecretKey { get; set; }

        public int? SettlementDay { get; set; }

        public SettlementIntervalType SettlementIntervalType { get; set; }

        public string? SmsVendorCode { get; set; }

        public string? SmsVendorSettings { get; set; }

        public Status Status { get; set; }

        public string? TimeZoneId { get; set; }
        public AgentDividendType AgentDividendType { get; set; }
    }
}
