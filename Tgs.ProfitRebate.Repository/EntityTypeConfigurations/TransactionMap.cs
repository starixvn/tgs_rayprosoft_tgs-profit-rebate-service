using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class TransactionMap : IEntityTypeConfiguration<Transaction>
    {
        void IEntityTypeConfiguration<Transaction>.Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("transactions");

            builder.HasKey(e => e.Id)
                .HasName("pk_transactions");

            builder.Property(c => c.PlayerCode)
                .IsRequired();

            builder.Property(c => c.Amount)
                .HasPrecision(18, 6);

            builder.Property(c => c.Balance)
                .HasPrecision(18, 6);

            builder.Property(c => c.Type)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(e => e.RecordedOn)
                .IsRequired();

            builder.Property(e => e.SentOn)
                .IsRequired(false);

            builder.HasIndex(e => e.PlayerCode)
                .HasDatabaseName("ix_transactions_player_code");

            builder
                .HasOne(e => e.Brand)
                .WithMany()
                .HasForeignKey(e => e.BrandId)
                .HasConstraintName("fk_transactions_brands_brand_id");

            builder.HasIndex(e => e.BrandId)
                .HasDatabaseName("ix_transactions_brand_id");

            builder
                .HasOne(e => e.Wallet)
                .WithMany()
                .HasForeignKey(e => e.WalletId)
                .HasConstraintName("fk_transactions_wallets_wallet_id");

            builder.HasIndex(e => e.WalletId)
                .HasDatabaseName("ix_transactions_wallet_id");
        }
    }
}