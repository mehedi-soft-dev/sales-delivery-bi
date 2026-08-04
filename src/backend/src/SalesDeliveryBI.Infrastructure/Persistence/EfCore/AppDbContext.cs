using Microsoft.EntityFrameworkCore;
using SalesDeliveryBI.Domain.Entities;

namespace SalesDeliveryBI.Infrastructure.Persistence.EfCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Buyer> Buyers => Set<Buyer>();
    public DbSet<Merchandiser> Merchandisers => Set<Merchandiser>();
    public DbSet<FxRate> FxRates => Set<FxRate>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
    public DbSet<QuotationStatusHistory> QuotationStatusHistories => Set<QuotationStatusHistory>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserUnit> UserUnits => Set<UserUnit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("sales");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
