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
        "Host=127.0.0.1;Port=5434;Database=salesdeliverybi;Username=salesdeliverybi;Password=salesdeliverybi;SSL Mode=Disable";

    public AppDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("SALESDELIVERYBI_CONNECTION")
            ?? _defaultConnectionString;

        // No MigrationsHistoryTable override — must match Infrastructure/DependencyInjection.cs's runtime
        // UseNpgsql(connectionString) call exactly (plain default: "__EFMigrationsHistory" in the "public"
        // schema). A design-time-only override here previously caused `dotnet ef database update` to record
        // applied migrations in a DIFFERENT history table (sales.__EFMigrationsHistory) than the one the
        // running app's own Database.MigrateAsync() checks at startup (Program.cs) — the app would see a
        // migration as "not yet applied" and try to re-run it, crashing on "relation already exists".
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
