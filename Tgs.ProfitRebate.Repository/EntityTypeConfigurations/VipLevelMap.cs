using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class VipLevelMap : BaseConfiguration, IEntityTypeConfiguration<VipLevel>
    {
        void IEntityTypeConfiguration<VipLevel>.Configure(EntityTypeBuilder<VipLevel> builder)
        {
            builder.ToTable("vip_levels");

            builder.HasKey(p => p.Id)
                .HasName("pk_vip_levels");

            builder.Property(c => c.LevelUpAccumulateBetAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.LevelUpAccumulateDeposit)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.LevelKeepAccumulateBetAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.LevelKeepAccumulateDeposit)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.BirthReward)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.PromotionReward)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.QuarterlyReward)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.LiveDealerRebatePercentage)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.SlotRebatePercentage)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.LotteryRebatePercentage)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.CardGameRebatePercentage)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.SportsRebatePercentage)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.VirtualSportsRebatePercentage)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.CreatedByUsername)
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(c => c.UpdatedByUsername)
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(c => c.CreatedOn)
                .IsRequired();

            builder.Property(c => c.UpdatedOn)
                .IsRequired();

            builder
                .HasOne(c => c.Brand)
                .WithMany(c => c.VipLevels)
                .HasForeignKey(c => c.BrandId)
                .HasConstraintName("fk_vip_levels_brands_brand_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => c.BrandId)
                .HasDatabaseName("ix_vip_levels_brand_id");
        }
    }
}