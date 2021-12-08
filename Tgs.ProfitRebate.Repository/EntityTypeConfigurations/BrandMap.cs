using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class BrandMap : BaseConfiguration, IEntityTypeConfiguration<Brand>
    {
        void IEntityTypeConfiguration<Brand>.Configure(EntityTypeBuilder<Brand> builder)
        {
            builder.ToTable("brands");

            ConfigureBaseProperties<Brand, Guid>(builder, "pk_brands");

            builder.Property(c => c.Status)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(c => c.Description)
                .HasMaxLength(256);

            builder.Property(c => c.OperationalEmail)
                .HasMaxLength(128);

            builder.Property(c => c.ClientId)
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(c => c.SecretKey)
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(c => c.ApiKey)
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(c => c.TimeZoneId)
                .HasMaxLength(256);

            builder.Property(c => c.EnableSms)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(b => b.EnableSmsRegisterFlow)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(c => c.SmsVendorCode);

            builder.Property(c => c.SmsVendorSettings)
                .HasColumnType(ColumnTypes.Jsonb);

            builder.Property(c => c.SettlementIntervalType)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(c => c.AgentDividendType)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(c => c.PreviousSettlementFrom)
                .IsRequired();

            builder.Property(c => c.CurrentSettlementFrom)
                .IsRequired();

            builder.Property(c => c.NextSettlementFrom)
                .IsRequired();

            builder.Property(c => c.PreviousSettlementTo)
                .IsRequired();

            builder.Property(c => c.CurrentSettlementTo)
                .IsRequired();

            builder.Property(c => c.NextSettlementTo)
                .IsRequired();

            builder.Property(c => c.LogoInfo)
                .HasColumnType(ColumnTypes.Jsonb);

            builder.Property(c => c.PlatformCharge)
                .HasPrecision(9, 6)
                .IsRequired();

            builder.HasOne(c => c.Licensee)
                .WithMany(c => c.Brands)
                .HasForeignKey(c => c.LicenseeId)
                .HasConstraintName("fk_brands_licensees_licensee_id");

            builder.HasIndex(c => c.LicenseeId)
                .HasDatabaseName("ix_brands_licensee_id");
        }
    }
}