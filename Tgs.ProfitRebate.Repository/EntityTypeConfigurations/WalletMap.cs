using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class WalletTypeMap : IEntityTypeConfiguration<Wallet>
    {
        void IEntityTypeConfiguration<Wallet>.Configure(EntityTypeBuilder<Wallet> builder)
        {
            builder.ToTable("wallets");

            builder.HasKey(e => e.Id)
                .HasName("pk_wallets");

            builder.Property(e => e.PlayerCode)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(e => e.CurrencyCode)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(e => e.ExternalCurrencyCode)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(e => e.Balance)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(e => e.CreatedOn)
                .IsRequired();

            builder.Property(e => e.UpdatedOn)
                .IsRequired();

            builder.HasOne(e => e.Player)
                .WithMany(e => e.Wallets)
                .HasForeignKey(e => e.PlayerCode)
                .HasConstraintName("fk_wallets_players_player_code")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Currency)
                .WithMany(e => e.Wallets)
                .HasForeignKey(e => e.CurrencyCode)
                .HasConstraintName("fk_wallets_currencies_currency_code")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => e.CurrencyCode)
                .HasDatabaseName("ix_wallets_currency_code");
        }
    }
}