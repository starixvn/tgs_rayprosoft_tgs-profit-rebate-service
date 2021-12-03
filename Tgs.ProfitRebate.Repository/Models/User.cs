using System;
using Tgs.ProfitRebate.Repository.Enums;

namespace Tgs.ProfitRebate.Repository.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public UserStatus Status { get; set; }
        public Guid RoleId { get; set; }
        public virtual Role Role { get; set; }
        public Guid DepartmentId { get; set; }
        public virtual Department Department { get; set; }
        public string Username { get; set; }
        public string LastLoginIp { get; set; }
        public DateTimeOffset? LastLoginTime { get; set; }
        public string Name { get; set; }
        public string TimeZone { get; set; }
        public byte[] Password { get; set; }
        public byte[] Salt { get; set; }
        public string CreatedByUsername { get; set; }
        public string UpdatedByUsername { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset UpdatedOn { get; set; }
    }
}