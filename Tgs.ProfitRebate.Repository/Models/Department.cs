using System;
using System.Collections.Generic;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class Department
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset UpdatedOn { get; set; }

        public string CreatedByUsername { get; set; }

        public string UpdatedByUsername { get; set; }

        public virtual ICollection<User> Users { get; set; }
        public virtual Guid? ResellerId { get; set; }
        public virtual Reseller Reseller { get; set; }
        public virtual Guid? LicenseeId { get; set; }
        public virtual Licensee Licensee { get; set; }
        public virtual Guid? BrandId { get; set; }
        public virtual Brand Brand { get; set; }
    }
}