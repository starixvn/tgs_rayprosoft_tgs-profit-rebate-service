using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class AgentMap : IEntityTypeConfiguration<Agent>
    {
        void IEntityTypeConfiguration<Agent>.Configure(EntityTypeBuilder<Agent> builder)
        {
            builder.ToTable("agents");

            builder.HasKey(b => b.Id)
                .HasName("pk_agents");

            builder.Property(c => c.Username)
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(c => c.RealName)
                .HasMaxLength(32);

            builder.Property(c => c.Status)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(c => c.Type)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(c => c.InvitationCode)
                .HasMaxLength(6)
                .IsRequired();

            builder.Property(c => c.Password)
                .IsRequired();

            builder.Property(c => c.Salt)
                .IsRequired();

            builder.Property(c => c.WithdrawPassword)
                .IsRequired();

            builder.Property(c => c.WithdrawSalt)
                .IsRequired();

            builder.Property(c => c.RegisteredOn)
                .IsRequired();

            builder.Property(c => c.Phone)
                .HasMaxLength(32);

            builder.Property(c => c.Email)
                .HasMaxLength(64);

            builder.Property(c => c.QQ)
                .HasMaxLength(64);

            builder.Property(c => c.Wechat)
                .HasMaxLength(64);

            builder.Property(c => c.CommissionPercentage)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.DividendPercentage)
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

            builder.HasOne(p => p.Brand)
                .WithMany()
                .HasForeignKey(p => p.BrandId)
                .HasConstraintName("fk_agents_brands_brand_id");

            builder.HasOne(p => p.ParentAgent)
                .WithMany(p => p.SubAgents)
                .HasForeignKey(p => p.ParentAgentId)
                .HasConstraintName("fk_agents_parent_agent_id")
                .IsRequired(false);

            builder.HasMany(p => p.AgentDividendConditions)
                .WithOne(p => p.Agent)
                .HasForeignKey(p => p.AgentId)
                .HasConstraintName("fk_agents_dividend_conditions_agent_id");

            builder.HasIndex(c => c.BrandId)
                .HasDatabaseName("ix_agents_brand_id");
        }
    }
}