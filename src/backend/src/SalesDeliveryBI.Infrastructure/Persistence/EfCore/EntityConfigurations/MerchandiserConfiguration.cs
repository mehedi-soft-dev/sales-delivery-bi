using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesDeliveryBI.Domain.Entities;

namespace SalesDeliveryBI.Infrastructure.Persistence.EfCore.EntityConfigurations;

public class MerchandiserConfiguration : IEntityTypeConfiguration<Merchandiser>
{
    public void Configure(EntityTypeBuilder<Merchandiser> builder)
    {
        builder.ToTable("Merchandisers", "sales");

        builder.Property(m => m.MerchandiserName).IsRequired().HasMaxLength(200);

        builder.HasOne(m => m.Unit)
            .WithMany()
            .HasForeignKey(m => m.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
