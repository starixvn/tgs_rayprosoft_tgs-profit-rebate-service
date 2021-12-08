using System;

namespace Tgs.ProfitRebate.Repository.Models
{
    public abstract class BaseEntity<T> where T : struct
    {
        public T Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset UpdatedOn { get; set; }
        public string CreatedByUsername { get; set; }
        public string UpdatedByUsername { get; set; }
    }
}