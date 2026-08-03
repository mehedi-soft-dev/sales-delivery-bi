using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesDeliveryBI.Domain.Entities;

namespace SalesDeliveryBI.Infrastructure.Persistence.EfCore.EntityConfigurations;

public class FxRateConfiguration : IEntityTypeConfiguration<FxRate>
{
    public void Configure(EntityTypeBuilder<FxRate> builder)
    {
        builder.ToTable("FxRates", "sales");

        builder.Property(f => f.CurrencyCode).IsRequired().HasMaxLength(3);
        builder.Property(f => f.RateToUsd).HasColumnType("numeric(18,6)");

        // One rate per currency per date — matches the snapshot-at-transaction-date rule (see database/schema-plan.md).
        builder.HasIndex(f => new { f.CurrencyCode, f.RateDate }).IsUnique();
    }
}
