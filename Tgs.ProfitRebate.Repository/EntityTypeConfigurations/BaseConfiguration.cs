using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal abstract class BaseConfiguration
    {
        protected static void ConfigureBaseProperties<TEntity, TId>(EntityTypeBuilder<TEntity> builder,
            string primaryKeyName = null)
            where TId : struct
            where TEntity : BaseEntity<TId>
        {
            builder.HasKey(p => p.Id)
                .HasName(primaryKeyName);

            builder.Property(c => c.Code)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(c => c.Name)
                .HasMaxLength(255)
                .IsRequired();

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