using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesDeliveryBI.Domain.Entities;

namespace SalesDeliveryBI.Infrastructure.Persistence.EfCore.EntityConfigurations;

public class UserUnitConfiguration : IEntityTypeConfiguration<UserUnit>
{
    public void Configure(EntityTypeBuilder<UserUnit> builder)
    {
        builder.ToTable("UserUnits", "sales");

        builder.HasIndex(uu => new { uu.UserId, uu.UnitId }).IsUnique();

        builder.HasOne(uu => uu.User)
            .WithMany(u => u.UserUnits)
            .HasForeignKey(uu => uu.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uu => uu.Unit)
            .WithMany()
            .HasForeignKey(uu => uu.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
