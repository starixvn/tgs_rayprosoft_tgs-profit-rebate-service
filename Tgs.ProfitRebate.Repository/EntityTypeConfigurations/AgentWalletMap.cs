using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class AgentWalletMap : IEntityTypeConfiguration<AgentWallet>
    {
        void IEntityTypeConfiguration<AgentWallet>.Configure(EntityTypeBuilder<AgentWallet> builder)
        {
            builder.ToTable("agent_wallets");

            builder.HasKey(e => e.Id)
                .HasName("pk_agent_wallets");

            builder.Property(e => e.CurrencyCode)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(e => e.Balance)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(e => e.CreatedOn)
                .IsRequired();

            builder.Property(e => e.UpdatedOn)
                .IsRequired();

            builder.HasOne(e => e.Agent)
                .WithMany(e => e.Wallets)
                .HasForeignKey(e => e.AgentId)
                .HasConstraintName("fk_agent_wallet_agent_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Currency)
                .WithMany()
                .HasForeignKey(e => e.CurrencyCode)
                .HasConstraintName("fk_agent_wallets_currencies_currency_code")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => e.CurrencyCode)
                .HasDatabaseName("ix_agent_wallets_currency_code");
        }
    }
}