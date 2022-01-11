using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class PlayerParentMap : IEntityTypeConfiguration<PlayerParent>
    {
        void IEntityTypeConfiguration<PlayerParent>.Configure(EntityTypeBuilder<PlayerParent> builder)
        {
            builder.ToTable("player_parents");

            builder.HasKey(bc => bc.PlayerCode)
                .HasName("pk_player_parent_code");

            builder.Property(c => c.PlayerCode)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(c => c.ParentPlayerCode)
                .HasMaxLength(16)
                .IsRequired();
        }
    }
}