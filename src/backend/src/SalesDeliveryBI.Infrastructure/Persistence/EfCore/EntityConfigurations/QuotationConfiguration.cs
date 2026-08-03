using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesDeliveryBI.Domain.Entities;

namespace SalesDeliveryBI.Infrastructure.Persistence.EfCore.EntityConfigurations;

public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable("Quotations", "sales");

        builder.Property(q => q.QuotationNo).IsRequired().HasMaxLength(50);
        builder.HasIndex(q => q.QuotationNo).IsUnique();

        builder.Property(q => q.StyleNo).IsRequired().HasMaxLength(100);
        builder.Property(q => q.Season).IsRequired().HasMaxLength(20);
        builder.Property(q => q.CurrencyCode).IsRequired().HasMaxLength(3);
        builder.Property(q => q.Value).HasColumnType("numeric(18,2)");

        builder.Property(q => q.Incoterm).IsRequired().HasMaxLength(10);
        builder.Property(q => q.PaymentTerm).IsRequired().HasMaxLength(50);
        builder.Property(q => q.Discount).HasColumnType("numeric(18,2)");

        builder.Property(q => q.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(q => q.ConvertedToSoNo).HasMaxLength(50);
        builder.Property(q => q.LostReason).HasMaxLength(500);

        builder.HasOne(q => q.Buyer)
            .WithMany()
            .HasForeignKey(q => q.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.Merchandiser)
            .WithMany()
            .HasForeignKey(q => q.MerchandiserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.Unit)
            .WithMany()
            .HasForeignKey(q => q.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Items/history are owned by their parent quotation, unlike the Buyer/Merchandiser/Unit lookups above.
        builder.HasMany(q => q.Items)
            .WithOne(i => i.Quotation)
            .HasForeignKey(i => i.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.StatusHistory)
            .WithOne(h => h.Quotation)
            .HasForeignKey(h => h.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
