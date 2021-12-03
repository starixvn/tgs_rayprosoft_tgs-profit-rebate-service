using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class CurrencyMap : IEntityTypeConfiguration<Currency>
    {
        void IEntityTypeConfiguration<Currency>.Configure(EntityTypeBuilder<Currency> builder)
        {
            builder.ToTable("currencies");

            builder.HasKey(p => p.Code)
                .HasName("pk_currencies");

            builder.Property(c => c.Code)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(c => c.Name)
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(c => c.NativeName)
                .HasMaxLength(64);

            builder.Property(c => c.Symbol)
                .HasMaxLength(6);

            builder.Property(c => c.CreatedByUsername)
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(c => c.UpdatedByUsername)
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(c => c.CreatedOn)
                .IsRequired();

            builder.Property(c => c.UpdatedOn)
                .IsRequired();
        }
    }
}