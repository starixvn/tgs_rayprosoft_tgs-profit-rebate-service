using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class LicenseeMap : BaseConfiguration, IEntityTypeConfiguration<Licensee>
    {
        void IEntityTypeConfiguration<Licensee>.Configure(EntityTypeBuilder<Licensee> builder)
        {
            builder.ToTable("licensees");

            ConfigureBaseProperties<Licensee, Guid>(builder, "pk_licensees");

            builder.Property(c => c.Status)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(c => c.Description)
                .HasMaxLength(256);

            builder.Property(c => c.OperationalEmail)
                .HasMaxLength(128);

            builder.HasOne(p => p.Reseller)
                .WithMany(p => p.Licensees)
                .HasForeignKey(p => p.ResellerId)
                .HasConstraintName("fk_licensees_resellers_reseller_id");

            builder.HasIndex(e => e.ResellerId)
                .HasDatabaseName("ix_licensees_reseller_id");
        }
    }
}