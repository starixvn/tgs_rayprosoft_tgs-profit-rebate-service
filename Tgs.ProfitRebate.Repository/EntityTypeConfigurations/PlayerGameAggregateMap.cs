using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class PlayerGameTypeAggregateMap : IEntityTypeConfiguration<PlayerGameAggregate>
    {
        void IEntityTypeConfiguration<PlayerGameAggregate>.Configure(
            EntityTypeBuilder<PlayerGameAggregate> builder)
        {
            builder.ToTable("player_game_rebate_aggregates", Consts.ReportSchema);

            builder.HasKey(b => b.Id)
                .HasName("pk_player_game_rebate_aggregates_id");

            builder.Property(b => b.Id)
                .ValueGeneratedOnAdd();

            builder.Property(c => c.PlayerCode)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(c => c.GameType)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(b => b.BetAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.ValidBetAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.OriginalTurnover)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.Turnover)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.WinLoss)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.VipLevel)
                .IsRequired(false);

            builder.Property(c => c.VipLiveDealerRebatePercentage)
                .HasPrecision(9, 6)
                .IsRequired();

            builder.Property(c => c.VipSlotRebatePercentage)
                .HasPrecision(9, 6)
                .IsRequired();

            builder.Property(c => c.VipCardGameRebatePercentage)
                .HasPrecision(9, 6)
                .IsRequired();

            builder.Property(c => c.VipSportsRebatePercentage)
                .HasPrecision(9, 6)
                .IsRequired();

            builder.Property(c => c.VipVirtualSportsRebatePercentage)
                .HasPrecision(9, 6)
                .IsRequired();

            builder.Property(c => c.VipLotteryRebatePercentage)
                .HasPrecision(9, 6)
                .IsRequired();

            builder.Property(c => c.VipRebateAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.ReportDate)
                .IsRequired();

            builder.Property(b => b.CreatedOn)
                .IsRequired();

            builder.HasIndex(b => new
            {
                b.ReportDate,
                b.BrandId,
                b.AgentId,
                b.PlayerCode
            })
                .HasDatabaseName("ix_player_game_rebate_aggregates_multiple_keys");
        }
    }
}