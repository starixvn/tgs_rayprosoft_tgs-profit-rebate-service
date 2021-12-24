using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class Reseller : BaseEntity<Guid>
    {
        public Status Status { get; set; }

        public string Description { get; set; }

        public string OperationalEmail { get; set; }
        public BusinessType BusinessType { get; set; }

        public virtual ICollection<Licensee> Licensees { get; set; }
    }
}