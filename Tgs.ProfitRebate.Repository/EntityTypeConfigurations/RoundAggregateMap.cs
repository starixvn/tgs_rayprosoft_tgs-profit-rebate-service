using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.ProfitRebate.Repository;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository.EntityTypeConfigurations
{
    internal class RoundAggregateMap : IEntityTypeConfiguration<RoundAggregate>
    {
        void IEntityTypeConfiguration<RoundAggregate>.Configure(EntityTypeBuilder<RoundAggregate> builder)
        {
            builder.ToTable("round_aggregates", Consts.ReportSchema);

            builder.HasKey(c => c.Id)
                .HasName("pk_round_aggregate_id");

            builder.Property(c => c.BrandId)
                .IsRequired();

            builder.Property(c => c.PlayerCode)
                .IsRequired();

            builder.Property(c => c.CreatedOn)
                .IsRequired();

            builder.Property(c => c.GameType)
                .HasColumnType(ColumnTypes.SmallInt)
                .IsRequired();

            builder.Property(c => c.BetAmount)
                .HasPrecision(18, 6);

            builder.Property(c => c.ValidBetAmount)
                .HasPrecision(18, 6);

            builder.Property(b => b.OriginalTurnover)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(c => c.Turnover)
                .HasPrecision(18, 6);

            builder.Property(c => c.WinLoss)
                .HasPrecision(18, 6);

            builder.HasIndex(c => new { c.PlayerCode })
                .HasDatabaseName("ix_round_aggregate_player_code");

            builder.HasIndex(c => new
            {
                c.ReportDate,
                c.Hour,
                c.Minute
            })
                .HasDatabaseName("ix_round_aggregate_multiple_keys");
        }
    }
}