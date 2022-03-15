using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class PlayerAggregateMap : IEntityTypeConfiguration<PlayerAggregate>
    {
        void IEntityTypeConfiguration<PlayerAggregate>.Configure(EntityTypeBuilder<PlayerAggregate> builder)
        {
            builder.ToTable("player_aggregates", Consts.ReportSchema);

            builder.HasKey(b => b.Id)
                .HasName("pk_player_aggregates_id");

            builder.Property(b => b.Id)
                .ValueGeneratedOnAdd();

            builder.Property(c => c.PlayerCode)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(b => b.RegisteredCount)
                .IsRequired();

            builder.Property(b => b.FirstFundInCount)
                .IsRequired();

            builder.Property(b => b.DepositAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.WithdrawalAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.CompanyAdjustAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.AgentAdjustAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.SystemAdjustAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.BonusAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.RebateAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.DividendAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.PromotionAmount)
                .HasPrecision(18, 6)
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

            builder.Property(b => b.WinLossAmount)
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
                .HasDatabaseName("ix_player_aggregates_multiple_keys");

            builder.HasIndex(b => new { b.AgentId })
                .HasDatabaseName("ix_player_aggregates_agent_id");
        }
    }
}