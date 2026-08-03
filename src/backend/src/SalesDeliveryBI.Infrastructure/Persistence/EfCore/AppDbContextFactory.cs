using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SalesDeliveryBI.Infrastructure.Persistence.EfCore;

/// <summary>
/// Used only by `dotnet ef` design-time tooling (migrations) — the running Api configures
/// AppDbContext through normal DI/appsettings instead (see Infrastructure/DependencyInjection.cs, Phase 10).
/// Connection string matches the local dev container in docs/plans/backend/local-environment.md.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string _defaultConnectionString =
        "Host=localhost;Port=5434;Database=salesdeliverybi;Username=salesdeliverybi;Password=salesdeliverybi";

    public AppDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("SALESDELIVERYBI_CONNECTION")
            ?? _defaultConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "sales"));

        return new AppDbContext(optionsBuilder.Options);
    }
}
