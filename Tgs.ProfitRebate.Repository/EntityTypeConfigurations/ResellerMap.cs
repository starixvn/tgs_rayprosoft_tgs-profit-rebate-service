using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class ResellerMap : BaseConfiguration, IEntityTypeConfiguration<Reseller>
    {
        void IEntityTypeConfiguration<Reseller>.Configure(EntityTypeBuilder<Reseller> builder)
        {
            builder.ToTable("resellers");

            ConfigureBaseProperties<Reseller, Guid>(builder, "pk_resellers");

            builder.Property(c => c.Status)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(c => c.Description)
                .HasMaxLength(256);

            builder.Property(c => c.OperationalEmail)
                .HasMaxLength(128);

            builder.Property(e => e.BusinessType)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();
        }
    }
}