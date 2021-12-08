using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    public sealed class UserMap : IEntityTypeConfiguration<User>
    {
        void IEntityTypeConfiguration<User>.Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(p => p.Id)
                .HasName("pk_users");

            builder.Property(c => c.Username)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(c => c.RoleId)
                .IsRequired();

            builder.Property(c => c.Status)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(c => c.Name)
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(c => c.TimeZone)
                .HasMaxLength(256)
                .IsRequired(false);

            builder.Property(c => c.LastLoginIp)
                .HasMaxLength(128)
                .IsRequired(false);

            builder.Property(c => c.Password)
                .IsRequired();

            builder.Property(c => c.Salt)
                .IsRequired();

            builder.Property(c => c.CreatedByUsername)
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(c => c.UpdatedByUsername)
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(c => c.LastLoginTime);

            builder.Property(c => c.CreatedOn)
                .IsRequired();

            builder.Property(c => c.UpdatedOn)
                .IsRequired();

            builder.HasOne(p => p.Role)
                .WithMany(p => p.Users)
                .HasForeignKey(p => p.RoleId)
                .HasConstraintName("fk_users_roles_role_id");

            builder.HasIndex(e => e.RoleId)
                .HasDatabaseName("ix_users_role_id");

            builder.HasOne(p => p.Department)
                .WithMany(p => p.Users)
                .HasForeignKey(p => p.DepartmentId)
                .HasConstraintName("fk_users_departments_department_id");

            builder.HasIndex(e => e.DepartmentId)
                .HasDatabaseName("ix_users_department_id");
        }
    }
}