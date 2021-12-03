using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class TransactionPromotionMap : IEntityTypeConfiguration<TransactionPromotion>
    {
        void IEntityTypeConfiguration<TransactionPromotion>.Configure(EntityTypeBuilder<TransactionPromotion> builder)
        {
            builder.ToTable("transaction_promotions");

            builder.HasKey(e => new { e.TransactionId, e.PromotionId })
                .HasName("pk_transaction_promotions");

            builder
                .HasOne(e => e.Transaction)
                .WithOne()
                .HasConstraintName("fk_transaction_promotion_transaction_id");

            builder.HasIndex(e => e.TransactionId)
                .HasDatabaseName("ix_transactions_transaction_id");

            builder
                .HasOne(e => e.Promotion)
                .WithMany()
                .HasConstraintName("fk_transaction_promotion_promotion_id");

            builder.HasIndex(e => e.PromotionId)
                .HasDatabaseName("ix_transaction_Promotion_id");
        }
    }
}