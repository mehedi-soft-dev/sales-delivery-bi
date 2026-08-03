using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesDeliveryBI.Domain.Entities;

namespace SalesDeliveryBI.Infrastructure.Persistence.EfCore.EntityConfigurations;

public class QuotationStatusHistoryConfiguration : IEntityTypeConfiguration<QuotationStatusHistory>
{
    public void Configure(EntityTypeBuilder<QuotationStatusHistory> builder)
    {
        builder.ToTable("QuotationStatusHistories", "sales");

        builder.Property(h => h.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(h => h.Note).HasMaxLength(500);

        builder.HasIndex(h => new { h.QuotationId, h.Status }).IsUnique();
    }
}
