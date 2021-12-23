using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class PlayerAllAgentRebateMap : IEntityTypeConfiguration<PlayerAllAgentRebate>
    {
        void IEntityTypeConfiguration<PlayerAllAgentRebate>.Configure(
            EntityTypeBuilder<PlayerAllAgentRebate> builder)
        {
            builder.ToTable("player_all_agent_rebates");

            builder.HasKey(b => b.Id)
                .HasName("pk_player_all_agent_rebates_id");

            builder.Property(b => b.Id)
                .ValueGeneratedOnAdd();

            builder.Property(c => c.PlayerCode)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(b => b.AllAgentGameType)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(b => b.GameType)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(b => b.AllAgentRebateType)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(c => c.ConditionAmount)
                .HasPrecision(18, 6)
                .IsRequired();
            builder.Property(b => b.RebatePercentage)
                .HasPrecision(9, 6)
                .IsRequired();
            builder.Property(c => c.ActualConditionAmount)
                .HasPrecision(18, 6)
                .IsRequired();
            builder.Property(c => c.RebateAmount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.ReportDate)
                .IsRequired();

            builder.Property(b => b.CreatedOn)
                .IsRequired();

            builder.HasIndex(b => new
                {
                    b.BrandId,
                    b.ReportDate,
                    b.PlayerCode
                })
                .HasDatabaseName("ix_player_all_agent_rebates_brand_id_report_date_player_code");
        }
    }
}