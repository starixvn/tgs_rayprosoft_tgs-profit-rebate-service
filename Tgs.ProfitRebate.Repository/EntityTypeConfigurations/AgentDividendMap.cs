using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class AgentDividendMap : IEntityTypeConfiguration<AgentDividend>
    {
        void IEntityTypeConfiguration<AgentDividend>.Configure(EntityTypeBuilder<AgentDividend> builder)
        {
            builder.ToTable("agent_dividends");

            builder.HasKey(b => b.Id)
                .HasName("pk_agent_dividends_id");

            builder.Property(b => b.Id)
                .ValueGeneratedOnAdd();

            builder.Property(b => b.PromotionAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.RebateAmount)
                .HasPrecision(18, 6)
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

            builder.Property(b => b.BonusAmount)
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

            builder.Property(b => b.NetProfit)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.LiveDealerWinLoss)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.SlotWinLoss)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.FishingWinLoss)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.ArcadeWinLoss)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.TableGameWinLoss)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.LotteryWinLoss)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.CardGameWinLoss)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.SportsWinLoss)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.ESportsWinLoss)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.VirtualSportsWinLoss)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.AgentDividendAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.AgentDividendPercentage)
                .HasPrecision(9, 6)
                .IsRequired();

            builder.Property(b => b.BeforeDividendAmount)
                .HasPrecision(18, 6);

            builder.Property(b => b.From)
                .IsRequired();

            builder.Property(b => b.To)
                .IsRequired();

            builder.Property(b => b.CreatedOn)
                .IsRequired();

            builder.Property(e => e.SentOn)
                .IsRequired(false);

            builder.HasIndex(b => new
                {
                    b.AgentId,
                    b.SentOn
                })
                .HasDatabaseName("ix_agent_dividends_agent_id_sent_on");

            builder.HasIndex(b => new
                {
                    b.BrandId,
                    b.From,
                    b.To
                })
                .HasDatabaseName("ix_agent_dividends_brand_id_from_to");

            builder.HasIndex(b => new
                {
                    b.AgentId,
                    b.BrandId,
                    b.CreatedOn
                })
                .HasDatabaseName("ix_agent_dividends_agent_id_brand_id_created_on");

            builder.HasMany(b => b.AgentDividendMultipleLayers)
                .WithOne(b => b.AgentDividend)
                .HasForeignKey(p => p.AgentDividendId)
                .HasConstraintName("fk_agents_dividends_agent_dividend_multiple_layers_agent_dividend_id");
        }
    }
}