using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    public class BrandAllAgentGameTypeConditionMap : IEntityTypeConfiguration<BrandAllAgentGameTypeCondition>
    {
        void IEntityTypeConfiguration<BrandAllAgentGameTypeCondition>.Configure(
            EntityTypeBuilder<BrandAllAgentGameTypeCondition> builder)
        {
            builder.ToTable("brand_all_agent_game_type_conditions");

            builder.HasKey(bc => new
                {
                    bc.BrandId,
                    GameType = bc.AllAgentGameType,
                    bc.Rank,
                    RebateType = bc.AllAgentRebateType
                })
                .HasName("pk_brand_all_agent_game_type_conditions");

            builder.Property(c => c.AllAgentRebateType)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(b => b.NetProfit)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.RebatePercentage)
                .HasPrecision(18, 6)
                .IsRequired();
        }
    }
}