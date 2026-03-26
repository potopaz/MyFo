using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Common;

namespace MyFO.Infrastructure.Persistence.Configurations.Common;

public class ExchangeRateSnapshotConfiguration : IEntityTypeConfiguration<ExchangeRateSnapshot>
{
    public void Configure(EntityTypeBuilder<ExchangeRateSnapshot> builder)
    {
        builder.ToTable("exchange_rate_snapshots", "cmn");

        builder.HasKey(e => new { e.BaseCurrency, e.TargetDate });

        builder.Property(e => e.BaseCurrency)
            .HasColumnName("base_currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(e => e.TargetDate)
            .HasColumnName("target_date")
            .IsRequired();

        builder.Property(e => e.RatesJson)
            .HasColumnName("rates_json")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.FetchedAt)
            .HasColumnName("fetched_at")
            .IsRequired();
    }
}
