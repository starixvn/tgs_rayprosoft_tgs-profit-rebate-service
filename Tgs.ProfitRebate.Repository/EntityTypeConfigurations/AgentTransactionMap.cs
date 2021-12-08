using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class AgentTransactionMap : IEntityTypeConfiguration<AgentTransaction>
    {
        void IEntityTypeConfiguration<AgentTransaction>.Configure(EntityTypeBuilder<AgentTransaction> builder)
        {
            builder.ToTable("agent_transactions");

            builder.HasKey(e => e.Id)
                .HasName("pk_agent_transactions");

            builder.Property(e => e.Amount)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(e => e.Balance)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.Type)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(e => e.RecordedOn)
                .IsRequired();

            builder.Property(e => e.SentOn)
                .IsRequired(false);

            builder.HasIndex(e => e.AgentId)
                .HasDatabaseName("ix_agent_transactions_agent_id");

            builder.HasOne(e => e.Wallet)
                .WithMany()
                .HasForeignKey(e => e.WalletId)
                .HasConstraintName("fk_agent_transaction_wallet_id");

            builder.HasIndex(e => e.WalletId)
                .HasDatabaseName("ix_agent_transactions_wallet_id");

            builder
                .HasOne(e => e.Brand)
                .WithMany()
                .HasForeignKey(e => e.BrandId)
                .HasConstraintName("fk_agent_transactions_brands_brand_id");

            builder.HasIndex(e => e.BrandId)
                .HasDatabaseName("ix_agent_transactions_brand_id");
        }
    }
}