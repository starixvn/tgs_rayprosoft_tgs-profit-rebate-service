using System;
using System.Collections.Generic;
using Tgs.ProfitRebate.Repository.Enums;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class Licensee : BaseEntity<Guid>
    {
        public Status Status { get; set; }

        public string Description { get; set; }

        public string OperationalEmail { get; set; }

        public Guid ResellerId { get; set; }

        public virtual Reseller Reseller { get; set; }

        public virtual ICollection<Brand> Brands { get; set; }
    }
}