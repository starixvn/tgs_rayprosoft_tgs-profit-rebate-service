using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class AgentDividendMultipleLayerMap : IEntityTypeConfiguration<AgentDividendMultipleLayer>
    {
        void IEntityTypeConfiguration<AgentDividendMultipleLayer>.Configure(
            EntityTypeBuilder<AgentDividendMultipleLayer> builder)
        {
            builder.ToTable("agent_dividend_multiple_layers");

            builder.HasKey(b => b.Id)
                .HasName("pk_agent_dividend_multiple_layers_id");

            builder.Property(b => b.Id)
                .ValueGeneratedOnAdd();

            builder.Property(b => b.Dividend)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(b => b.AgentId)
                .IsRequired();

            builder.Property(b => b.DividendProviderAgentId)
                .IsRequired();

            builder.Property(b => b.Layer)
                .IsRequired();

            builder.HasIndex(b => new
                {
                    b.AgentId,
                    b.DividendProviderAgentId
                })
                .HasDatabaseName("ix_agent_dividend_multiple_layers_agent_id_dividend_provider_agent_id");
        }
    }
}