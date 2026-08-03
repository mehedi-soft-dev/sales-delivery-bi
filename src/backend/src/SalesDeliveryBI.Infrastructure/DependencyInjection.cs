using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Infrastructure.Persistence.EfCore;
using SalesDeliveryBI.Infrastructure.Persistence.EfCore.Seed;
using SalesDeliveryBI.Infrastructure.Security;

namespace SalesDeliveryBI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Phase 7 replaces this with a request-scoped, JWT-claims-based implementation.
        services.AddSingleton<ICurrentUserContext, SystemCurrentUserContext>();

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            string connectionString = configuration.GetConnectionString("SalesDeliveryBi")
                ?? throw new InvalidOperationException("Missing 'ConnectionStrings:SalesDeliveryBi' configuration.");

            options.UseNpgsql(connectionString)
                .AddInterceptors(provider.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
